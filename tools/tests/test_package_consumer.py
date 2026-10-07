from pathlib import Path
import json
import os
import subprocess
import sys
import tempfile
import unittest

from package_fixtures import package_pair

CONSUMER = Path(__file__).resolve().parents[1] / 'test-package-consumer.sh'


class PackageConsumerTests(unittest.TestCase):
    def setUp(self):
        self.scratch = tempfile.TemporaryDirectory(prefix='pagecurl-feed-')
        self.addCleanup(self.scratch.cleanup)
        self.root = Path(self.scratch.name)
        self.feed = self.root / 'feed with spaces & symbols'
        self.log = self.root / 'calls.jsonl'
        self.cli = self.root / 'dotnet-test'
        # The real shell/ZIP/XML path runs; only the external .NET process is replaced.
        self.cli.write_text(f'''#!{sys.executable}
import json, os, pathlib, sys, xml.etree.ElementTree as ET
args = sys.argv[1:]
if args[0] == 'pack':
    sys.exit('Unexpected repack instead of consuming supplied feed')
project = pathlib.Path(args[1]) if args[0] == 'restore' else pathlib.Path(args[args.index('--project') + 1])
if args[0] == 'restore':
    ET.parse(args[args.index('--configfile') + 1])
with open(os.environ['PAGECURL_TEST_CALLS'], 'a') as log:
    log.write(json.dumps({{'args': args, 'project': project.read_text()}}) + '\\n')
''')
        self.cli.chmod(0o755)
        temporary = self.root / 'scratch with spaces & symbols'
        temporary.mkdir()
        self.env = os.environ | {'PAGECURL_DOTNET': str(self.cli), 'PAGECURL_TEST_CALLS': str(self.log), 'TMPDIR': str(temporary)}
        package_pair(self.feed, 'PageCurl.Core', ['net10.0'], version='0.2.0')
        package_pair(self.feed, 'PageCurl.Maui', ['net10.0'], version='0.2.0')

    def run_consumer(self):
        return subprocess.run(['bash', str(CONSUMER), str(self.feed)], env=self.env,
                              capture_output=True, text=True)

    def test_consumes_supplied_feed_without_repacking_and_uses_its_version(self):
        result = self.run_consumer()
        self.assertEqual(result.returncode, 0, result.stderr)
        calls = [json.loads(line) for line in self.log.read_text().splitlines()]
        self.assertEqual([call['args'][0] for call in calls], ['restore', 'run'])
        self.assertIn('Version="0.2.0"', calls[0]['project'])
        self.assertNotIn('ProjectReference', calls[0]['project'])
        self.assertIn('--no-cache', calls[0]['args'])
        project = Path(calls[0]['args'][1])
        self.assertFalse(project.exists(), 'Consumer scratch must be cleaned up')
        self.assertEqual(sorted(path.name for path in self.feed.iterdir()),
                         ['PageCurl.Core.0.2.0.nupkg', 'PageCurl.Core.0.2.0.snupkg', 'PageCurl.Maui.0.2.0.nupkg', 'PageCurl.Maui.0.2.0.snupkg'])

    def test_rejects_multiple_package_versions_before_dotnet(self):
        package_pair(self.feed, 'PageCurl.Maui', ['net10.0'], version='0.3.0')
        result = self.run_consumer()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Expected one Core and one MAUI package', result.stderr)
        self.assertFalse(self.log.exists())

    def test_rejects_mixed_core_and_maui_versions_before_dotnet(self):
        for path in self.feed.glob('PageCurl.Core.*'):
            path.unlink()
        package_pair(self.feed, 'PageCurl.Core', ['net10.0'], version='0.1.0')
        result = self.run_consumer()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Core and MAUI release identities differ', result.stderr)
        self.assertFalse(self.log.exists())

    def test_rejects_missing_feed_before_dotnet(self):
        self.feed = self.root / 'missing'
        result = self.run_consumer()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Package feed directory not found', result.stderr)
        self.assertFalse(self.log.exists())
