from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


EXTRACTOR = Path(__file__).parents[1] / 'extract-doc-example.py'
XAML = '<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui" xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml" x:Class="ReaderApp.ReaderPage"><Grid /></ContentPage>'
CODE = 'namespace ReaderApp;\npublic partial class ReaderPage : ContentPage { }'


class DocExampleTests(unittest.TestCase):
    def extract(self, guide, output):
        source = output.parent / 'guide.md'
        source.write_text(guide)
        return subprocess.run([sys.executable, str(EXTRACTOR), str(source), str(output)],
                              capture_output=True, text=True)

    def test_extracts_exact_complete_page_and_matching_code(self):
        with tempfile.TemporaryDirectory() as folder:
            output = Path(folder) / 'example'
            guide = f'```xml\n<curl:PageSpreadView />\n```\n```xml\n{XAML}\n```\n```csharp\n{CODE}\n```'
            result = self.extract(guide, output)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual(XAML + '\n', (output / 'ReaderPage.xaml').read_text())
            self.assertEqual(CODE + '\n', (output / 'ReaderPage.xaml.cs').read_text())
            stamp = (output / 'ReaderPage.xaml.cs').stat().st_mtime_ns
            self.assertEqual(0, self.extract(guide + '\nMore prose.', output).returncode)
            self.assertEqual(stamp, (output / 'ReaderPage.xaml.cs').stat().st_mtime_ns)

    def test_rejects_missing_ambiguous_or_mismatched_examples_without_output(self):
        complete = f'```xml\n{XAML}\n```\n```csharp\n{CODE}\n```'
        cases = [
            '```xml\n<curl:PageSpreadView />\n```',
            f'```xml\n{XAML}\n```',
            complete + '\n' + complete,
            complete.replace('namespace ReaderApp;', 'namespace AnotherApp;'),
            complete.replace('<Grid />', '<Grid>'),
        ]
        for guide in cases:
            with self.subTest(guide=guide), tempfile.TemporaryDirectory() as folder:
                output = Path(folder) / 'example'
                result = self.extract(guide, output)
                self.assertNotEqual(0, result.returncode)
                self.assertFalse(output.exists())
