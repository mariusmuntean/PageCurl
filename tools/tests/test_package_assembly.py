"""Exercise host package assembly with real NuGet-format ZIP inputs."""
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile

ASSEMBLER = Path(__file__).resolve().parents[1] / 'assemble-packages.py'
COMMIT = '0123456789012345678901234567890123456789'
FRAMEWORKS = ['net10.0', 'net10.0-android36.0', 'net10.0-ios26.0', 'net10.0-maccatalyst26.0']
WINDOWS = 'net10.0-windows10.0.19041.0'


def package_pair(folder, package_id, frameworks, commit=COMMIT, version='0.1.0', readme='# PageCurl'):
    folder.mkdir(parents=True, exist_ok=True)
    for suffix in ['nupkg', 'snupkg']:
        path = folder / f'{package_id}.{version}.{suffix}'
        groups = ''.join(f'<group targetFramework="{tfm}"><dependency id="PageCurl.Core" version="{version}" /></group>' for tfm in frameworks) if package_id == 'PageCurl.Maui' else ''
        common = f'<id>{package_id}</id><version>{version}</version><description>Page turns</description><projectUrl>https://mariusmuntean.github.io/PageCurl-docs/</projectUrl><repository type="git" url="https://github.com/mariusmuntean/PageCurl" commit="{commit}" />'
        metadata = common + ('<packageTypes><packageType name="SymbolsPackage" /></packageTypes>' if suffix == 'snupkg' else '<authors>Marius Muntean</authors><license type="expression">MIT</license><readme>README.md</readme>')
        with zipfile.ZipFile(path, 'w') as archive:
            archive.writestr(f'{package_id}.nuspec', f'<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata>{metadata}<dependencies>{groups}</dependencies></metadata></package>')
            archive.writestr('_rels/.rels', f'<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Type="http://schemas.microsoft.com/packaging/2010/07/manifest" Target="/{package_id}.nuspec" Id="manifest" /><Relationship Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="/package/services/metadata/core-properties/nuget.psmdcp" Id="properties" /></Relationships>')
            archive.writestr('package/services/metadata/core-properties/nuget.psmdcp', '<properties/>')
            archive.writestr('[Content_Types].xml', '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="nuspec" ContentType="application/octet" /><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" /><Default Extension="psmdcp" ContentType="application/vnd.openxmlformats-package.core-properties+xml" /><Default Extension="dll" ContentType="application/octet" /><Default Extension="xml" ContentType="application/octet" /><Default Extension="md" ContentType="application/octet" /><Default Extension="pdb" ContentType="application/octet" /></Types>')
            if suffix == 'nupkg':
                archive.writestr('README.md', readme)
            for tfm in frameworks:
                extensions = ['pdb'] if suffix == 'snupkg' else ['dll', 'xml']
                for extension in extensions:
                    archive.writestr(f'lib/{tfm}/{package_id}.{extension}', f'{tfm} {extension}')


def rewrite(path, changes):
    with zipfile.ZipFile(path) as archive:
        entries = {name: archive.read(name) for name in archive.namelist()}
    entries.update(changes)
    with zipfile.ZipFile(path, 'w') as archive:
        for name, data in entries.items():
            if data is not None:
                archive.writestr(name, data)


class PackageAssemblyTests(unittest.TestCase):
    def setUp(self):
        self.scratch = tempfile.TemporaryDirectory(prefix='pagecurl-assembly-')
        self.addCleanup(self.scratch.cleanup)
        self.root = Path(self.scratch.name)
        self.mobile = self.root / 'mobile'
        self.windows = self.root / 'windows'
        self.output = self.root / 'distribution'
        package_pair(self.mobile, 'PageCurl.Core', ['net10.0'])
        package_pair(self.mobile, 'PageCurl.Maui', FRAMEWORKS)
        package_pair(self.windows, 'PageCurl.Maui', [WINDOWS])

    def assemble(self, directories=None, output=None):
        return subprocess.run([sys.executable, str(ASSEMBLER), '--output', str(output or self.output),
                               *map(str, directories or [self.mobile, self.windows])],
                              capture_output=True, text=True)

    def assert_rejected(self, fragment):
        result = self.assemble()
        self.assertNotEqual(result.returncode, 0, result.stdout)
        self.assertIn(fragment, result.stderr)
        self.assertFalse(list(self.output.glob('*')), 'Failed assembly must not leave distributable files')

    def test_combines_platform_payload_dependencies_and_symbols(self):
        result = self.assemble()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(sorted(path.name for path in self.output.iterdir()),
                         ['PageCurl.Core.0.1.0.nupkg', 'PageCurl.Core.0.1.0.snupkg', 'PageCurl.Maui.0.1.0.nupkg', 'PageCurl.Maui.0.1.0.snupkg'])
        for suffix, extension in [('nupkg', 'dll'), ('snupkg', 'pdb')]:
            with zipfile.ZipFile(self.output / f'PageCurl.Maui.0.1.0.{suffix}') as archive:
                for tfm in FRAMEWORKS + [WINDOWS]:
                    self.assertEqual(archive.read(f'lib/{tfm}/PageCurl.Maui.{extension}'), f'{tfm} {extension}'.encode())
                root = ET.fromstring(archive.read('PageCurl.Maui.nuspec'))
                groups = [node.get('targetFramework') for node in root.iter() if node.tag.split('}')[-1] == 'group']
                self.assertEqual(sorted(groups), sorted(FRAMEWORKS + [WINDOWS]))

    def test_rejects_incomplete_distribution(self):
        package_pair(self.windows, 'PageCurl.Maui', ['net10.0'])
        self.assert_rejected('Missing framework net10.0-windows')

    def test_rejects_mixed_source_revisions(self):
        package_pair(self.windows, 'PageCurl.Maui', [WINDOWS], commit='a' * 40)
        self.assert_rejected('metadata differs')

    def test_rejects_mixed_versions(self):
        for path in self.windows.iterdir():
            path.unlink()
        package_pair(self.windows, 'PageCurl.Maui', [WINDOWS], version='0.2.0')
        self.assert_rejected('metadata differs')

    def test_rejects_conflicting_shared_asset(self):
        package_pair(self.windows, 'PageCurl.Maui', [WINDOWS], readme='different README')
        self.assert_rejected('Conflicting package entry README.md')

    def test_rejects_signed_input(self):
        rewrite(self.windows / 'PageCurl.Maui.0.1.0.nupkg', {'.signature.p7s': b'signature'})
        self.assert_rejected('Signed packages cannot be assembled')

    def test_rejects_missing_host_symbols(self):
        (self.windows / 'PageCurl.Maui.0.1.0.snupkg').unlink()
        self.assert_rejected('Missing symbol package')

    def test_rejects_missing_platform_symbol(self):
        rewrite(self.windows / 'PageCurl.Maui.0.1.0.snupkg', {f'lib/{WINDOWS}/PageCurl.Maui.pdb': None})
        self.assert_rejected('Missing symbol asset')

    def test_rejects_symbols_from_a_different_revision(self):
        symbols = self.windows / 'PageCurl.Maui.0.1.0.snupkg'
        with zipfile.ZipFile(symbols) as archive:
            manifest = archive.read('PageCurl.Maui.nuspec').replace(COMMIT.encode(), ('a' * 40).encode())
        rewrite(symbols, {'PageCurl.Maui.nuspec': manifest})
        self.assert_rejected('Symbol identity differs')

    def test_rejects_core_from_a_different_revision(self):
        package_pair(self.mobile, 'PageCurl.Core', ['net10.0'], commit='a' * 40)
        self.assert_rejected('Core release identity differs')

    def test_rejects_conflicting_dependency_groups(self):
        package_pair(self.windows, 'PageCurl.Maui', [WINDOWS, 'net10.0'])
        path = self.windows / 'PageCurl.Maui.0.1.0.nupkg'
        with zipfile.ZipFile(path) as archive:
            manifest = archive.read('PageCurl.Maui.nuspec').replace(b'version="0.1.0"', b'version="0.0.9"')
        rewrite(path, {'PageCurl.Maui.nuspec': manifest})
        self.assert_rejected('Conflicting dependencies for net10.0')

    def test_refuses_to_overwrite_existing_distribution(self):
        self.output.mkdir()
        prior = self.output / 'PageCurl.Maui.0.1.0.nupkg'
        prior.write_bytes(b'existing release')
        result = self.assemble()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('already exists', result.stderr)
        self.assertEqual(prior.read_bytes(), b'existing release')

    def test_rejects_stale_package_versions_in_output(self):
        self.output.mkdir()
        prior = self.output / 'PageCurl.Maui.0.0.9.nupkg'
        prior.write_bytes(b'older release')
        result = self.assemble()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('empty output directory', result.stderr)
        self.assertEqual(list(self.output.iterdir()), [prior])
        self.assertEqual(prior.read_bytes(), b'older release')

    def test_accepts_empty_output_directory(self):
        self.output.mkdir()
        result = self.assemble()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(len(list(self.output.iterdir())), 4)
        self.assertFalse(list(self.root.glob('.pagecurl-package-*')))

    def test_reversed_inputs_produce_identical_packages(self):
        result = self.assemble()
        self.assertEqual(result.returncode, 0, result.stderr)
        other = self.root / 'other'
        result = self.assemble([self.windows, self.mobile], other)
        self.assertEqual(result.returncode, 0, result.stderr)
        for path in self.output.iterdir():
            self.assertEqual(path.read_bytes(), (other / path.name).read_bytes())

    def test_preserves_platform_content_type_for_new_assets(self):
        path = self.windows / 'PageCurl.Maui.0.1.0.nupkg'
        with zipfile.ZipFile(path) as archive:
            types = archive.read('[Content_Types].xml').replace(b'</Types>', b'<Default Extension="pri" ContentType="application/octet" /></Types>')
        rewrite(path, {f'lib/{WINDOWS}/PageCurl.Maui.pri': b'resources', '[Content_Types].xml': types})
        result = self.assemble()
        self.assertEqual(result.returncode, 0, result.stderr)
        with zipfile.ZipFile(self.output / 'PageCurl.Maui.0.1.0.nupkg') as archive:
            self.assertEqual(archive.read(f'lib/{WINDOWS}/PageCurl.Maui.pri'), b'resources')
            types = ET.fromstring(archive.read('[Content_Types].xml'))
            self.assertTrue(any(node.get('Extension') == 'pri' for node in types))


    def test_rejects_case_collisions_between_host_assets(self):
        rewrite(self.windows / 'PageCurl.Maui.0.1.0.nupkg', {'README.md': None, 'readme.md': b'conflicting casing'})
        self.assert_rejected('Conflicting package entry casing')

    def test_rejects_unknown_host_relationships(self):
        path = self.windows / 'PageCurl.Maui.0.1.0.nupkg'
        with zipfile.ZipFile(path) as archive:
            relationships = archive.read('_rels/.rels').replace(b'</Relationships>', b'<Relationship Type="custom" Target="/custom.xml" Id="extra" /></Relationships>')
        rewrite(path, {'_rels/.rels': relationships, 'custom.xml': b'custom'})
        self.assert_rejected('Unsupported package relationship')

    def test_rejects_conflicting_content_types(self):
        path = self.windows / 'PageCurl.Maui.0.1.0.nupkg'
        with zipfile.ZipFile(path) as archive:
            types = archive.read('[Content_Types].xml').replace(b'Extension="dll" ContentType="application/octet"', b'Extension="dll" ContentType="text/plain"')
        rewrite(path, {'[Content_Types].xml': types})
        self.assert_rejected('Conflicting content type dll')
