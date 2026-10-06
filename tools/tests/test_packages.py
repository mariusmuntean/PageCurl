import importlib.util
from pathlib import Path
import tempfile
import unittest
import warnings
import zipfile

spec = importlib.util.spec_from_file_location('check_packages', Path(__file__).parents[1] / 'check-packages.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def package(path, frameworks, missing_xml=False, missing_core=False, core_version="0.1.0"):
    dependency = '' if missing_core else f'<dependency id="PageCurl.Core" version="{core_version}" />'
    groups = ''.join(f'<group targetFramework="{tfm}">{dependency}</group>' for tfm in frameworks)
    metadata = '<id>PageCurl.Maui</id><version>0.1.0</version><description>Page turns</description><license type="expression">MIT</license><readme>README.md</readme><projectUrl>https://mariusmuntean.github.io/PageCurl-docs/</projectUrl><repository type="git" url="https://github.com/mariusmuntean/PageCurl" commit="0123456789012345678901234567890123456789" />'
    with zipfile.ZipFile(path, 'w') as archive:
        archive.writestr('PageCurl.Maui.nuspec', f'<package><metadata>{metadata}<dependencies>{groups}</dependencies></metadata></package>')
        archive.writestr('_rels/.rels', '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Type="http://schemas.microsoft.com/packaging/2010/07/manifest" Target="/PageCurl.Maui.nuspec" Id="manifest" /><Relationship Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="/package/services/metadata/core-properties/nuget.psmdcp" Id="properties" /></Relationships>')
        archive.writestr('package/services/metadata/core-properties/nuget.psmdcp', '<properties/>')
        archive.writestr('[Content_Types].xml', '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types" />')
        archive.writestr('README.md', '# PageCurl')
        for tfm in frameworks:
            archive.writestr(f'lib/{tfm}/PageCurl.Maui.dll', b'assembly')
            if not missing_xml:
                archive.writestr(f'lib/{tfm}/PageCurl.Maui.xml', '<doc/>')


class PackageTests(unittest.TestCase):
    def check(self, frameworks, expected, **kwargs):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'test.nupkg'
            package(path, frameworks, **kwargs)
            return module.check_package(path, expected)

    def test_accepts_platform_versioned_assets(self):
        self.assertEqual(self.check(['net10.0', 'net10.0-ios27.0'], ['net10.0', 'net10.0-ios']), [])

    def test_requires_neutral_asset_separately(self):
        self.assertIn('Missing framework net10.0', self.check(['net10.0-ios27.0'], ['net10.0']))

    def test_rejects_missing_platform_and_xml(self):
        self.assertIn('Missing framework net10.0-android', self.check(['net10.0'], ['net10.0-android']))
        self.assertTrue(any('XML' in error for error in self.check(['net10.0'], ['net10.0'], missing_xml=True)))

    def test_requires_core_dependency_for_each_framework(self):
        self.assertTrue(any('Core dependency' in error for error in self.check(['net10.0'], ['net10.0'], missing_core=True)))


    def test_rejects_invalid_platform_suffix(self):
        self.assertIn('Missing framework net10.0-ios',
                      self.check(['net10.0-ios-not-a-framework'], ['net10.0-ios']))

    def test_rejects_dependency_from_a_different_release(self):
        self.assertTrue(any('Core dependency version' in error for error in
                            self.check(['net10.0'], ['net10.0'], core_version='0.0.9')))

    def test_malformed_manifest_reports_failure(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'test.nupkg'
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr('PageCurl.Maui.nuspec', '<package/>')
            self.assertTrue(module.check_package(path, ['net10.0']))

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
            with zipfile.ZipFile(path) as archive:
                entries = {name: archive.read(name) for name in archive.namelist() if name != '_rels/.rels'}
            with zipfile.ZipFile(path, 'w') as archive:
                for name, payload in entries.items():
                    archive.writestr(name, payload)
            self.assertTrue(any('Missing _rels/.rels' in error for error in module.check_package(path, ['net10.0'])))
