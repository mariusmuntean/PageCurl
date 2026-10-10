import importlib.util
from pathlib import Path
import tempfile
import unittest
import warnings
import xml.etree.ElementTree as ET
import zipfile

from package_fixtures import package, package_icon, rewrite

spec = importlib.util.spec_from_file_location('check_packages', Path(__file__).parents[1] / 'check-packages.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PackageTests(unittest.TestCase):
    def check(self, frameworks, expected, **kwargs):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'test.nupkg'
            package(path, frameworks, **kwargs)
            return module.check_package(path, expected)

    def test_accepts_platform_versioned_assets(self):
        for namespaced in (False, True):
            self.assertEqual(self.check(['net10.0', 'net10.0-ios27.0'], ['net10.0', 'net10.0-ios'], namespaced=namespaced), [])

    def test_requires_neutral_asset_separately(self):
        self.assertIn('Missing framework net10.0', self.check(['net10.0-ios27.0'], ['net10.0']))

    def test_rejects_missing_platform_and_xml(self):
        self.assertIn('Missing framework net10.0-android', self.check(['net10.0'], ['net10.0-android']))
        self.assertTrue(any('XML' in error for error in self.check(['net10.0'], ['net10.0'], missing_xml=True)))

    def test_requires_core_dependency_for_each_framework(self):
        self.assertTrue(any('Core dependency' in error for error in self.check(['net10.0'], ['net10.0'], missing_core=True)))

    def test_requires_square_png_package_icon(self):
        self.assertEqual(self.check(['net10.0'], ['net10.0'], package_id='PageCurl.Core'), [])
        self.assertIn('Missing package icon', self.check(['net10.0'], ['net10.0'], include_icon=False))
        self.assertIn('Missing package icon',
                      self.check(['net10.0'], ['net10.0'], package_id='PageCurl.Core', include_icon=False))
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'test.nupkg'
            package(path, ['net10.0'])
            rewrite(path, {'pagecurl-icon.png': b'not a png'})
            self.assertIn('Invalid package icon PNG', module.check_package(path, ['net10.0']))
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'test.nupkg'
            package(path, ['net10.0'])
            rewrite(path, {'pagecurl-icon.png': package_icon(512, 256)})
            self.assertIn('Invalid package icon PNG', module.check_package(path, ['net10.0']))


    def test_rejects_invalid_platform_suffix(self):
        self.assertIn('Missing framework net10.0-ios',
                      self.check(['net10.0-ios-not-a-framework'], ['net10.0-ios']))

    def test_rejects_dependency_from_a_different_release(self):
        self.assertTrue(any('Core dependency version' in error for error in
                            self.check(['net10.0'], ['net10.0'], core_version='0.0.9')))

    def test_malformed_manifest_reports_failure(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'test.nupkg'
            package(path, ['net10.0'])
            rewrite(path, {'PageCurl.Maui.nuspec': '<package/>'})
            self.assertEqual(module.check_package(path, ['net10.0']),
                             ['Invalid package test.nupkg: Missing package metadata in test.nupkg'])

    def test_corrupt_archive_reports_failure(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'test.nupkg'
            path.write_bytes(b'not a package')
            self.assertTrue(module.check_package(path, ['net10.0']))

    def test_duplicate_archive_entries_are_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'test.nupkg'
            package(path, ['net10.0'])
            with warnings.catch_warnings():
                warnings.simplefilter('ignore', UserWarning)
                with zipfile.ZipFile(path, 'a') as archive:
                    archive.writestr('lib/net10.0/PageCurl.Maui.dll', b'replaced')
            self.assertTrue(any('Duplicate' in error for error in module.check_package(path, ['net10.0'])))

    def test_rejects_unsafe_archive_members(self):
        for name, diagnostic in [
            ('../../outside', 'Invalid package entry'),
            ('/absolute', 'Invalid package entry'),
            ('lib\\outside', 'Invalid package entry'),
            ('readme.md', 'Duplicate package entry'),
            ('.signature.p7s', 'Signed packages'),
        ]:
            with self.subTest(name=name), tempfile.TemporaryDirectory() as folder:
                path = Path(folder) / 'test.nupkg'
                package(path, ['net10.0'])
                with zipfile.ZipFile(path, 'a') as archive:
                    archive.writestr(name, b'payload')
                self.assertTrue(any(diagnostic in error for error in module.check_package(path, ['net10.0'])))

    def test_requires_package_relationships(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'test.nupkg'
            package(path, ['net10.0'])
            rewrite(path, {'_rels/.rels': None})
            self.assertTrue(any('Missing _rels/.rels' in error for error in module.check_package(path, ['net10.0'])))

    def test_rejects_invalid_opc_parts(self):
        namespaces = {
            '_rels/.rels': 'http://schemas.openxmlformats.org/package/2006/relationships',
            '[Content_Types].xml': 'http://schemas.openxmlformats.org/package/2006/content-types',
        }
        cases = [(part, level, namespace) for part in namespaces
                 for level in ('root', 'child') for namespace in ('', 'urn:invalid')]
        cases += [(part, level, 'Unexpected') for part in namespaces for level in ('root-name', 'child-name')]
        cases += [(part, attribute, value) for part, attribute in [
            ('_rels/.rels', 'Relationship.Id'),
            ('[Content_Types].xml', 'Default.Extension'),
            ('[Content_Types].xml', 'Default.ContentType'),
            ('[Content_Types].xml', 'Override.PartName'),
            ('[Content_Types].xml', 'Override.ContentType'),
        ] for value in (None, '')]
        for part, location, value in cases:
            with self.subTest(part=part, location=location, value=value), tempfile.TemporaryDirectory() as folder:
                path = Path(folder) / 'test.nupkg'
                package(path, ['net10.0'])
                with zipfile.ZipFile(path) as archive:
                    root = ET.fromstring(archive.read(part))
                if location in ('root', 'child', 'root-name', 'child-name'):
                    node = root if location.startswith('root') else root[0]
                    namespace, name = (value, node.tag.split('}')[-1]) if '-' not in location else (namespaces[part], value)
                    node.tag = f'{{{namespace}}}{name}' if namespace else name
                else:
                    kind, attribute = location.split('.')
                    node = next((child for child in root if child.tag.split('}')[-1] == kind), None)
                    if node is None:
                        node = ET.SubElement(root, f'{{{namespaces[part]}}}Override', PartName='/README.md', ContentType='application/octet')
                    if value is None:
                        node.attrib.pop(attribute)
                    else:
                        node.set(attribute, value)
                rewrite(path, {part: ET.tostring(root)})
                self.assertTrue(any(f'Invalid OPC part {part}' in error for error in module.check_package(path, ['net10.0'])))
