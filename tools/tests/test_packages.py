import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

spec = importlib.util.spec_from_file_location('check_packages', Path(__file__).parents[1] / 'check-packages.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def package(path, frameworks, missing_xml=False, missing_core=False):
    dependency = '' if missing_core else '<dependency id="PageCurl.Core" version="0.1.0" />'
    groups = ''.join(f'<group targetFramework="{tfm}">{dependency}</group>' for tfm in frameworks)
    metadata = '<id>PageCurl.Maui</id><version>0.1.0</version><description>Page turns</description><license type="expression">MIT</license><readme>README.md</readme><projectUrl>https://mariusmuntean.github.io/PageCurl-docs/</projectUrl>'
    with zipfile.ZipFile(path, 'w') as archive:
        archive.writestr('PageCurl.Maui.nuspec', f'<package><metadata>{metadata}<dependencies>{groups}</dependencies></metadata></package>')
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
