"""Require identical packaged text on Windows and Unix checkouts."""
from pathlib import Path
import os
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]


class PackageLineEndingTests(unittest.TestCase):
    def test_windows_checkout_preserves_packaged_text_bytes(self):
        with tempfile.TemporaryDirectory(prefix='pagecurl-line-endings-') as folder:
            worktree = Path(folder)
            env = os.environ | {
                'GIT_DIR': str(worktree / '.git'),
                'GIT_WORK_TREE': str(worktree),
                'GIT_INDEX_FILE': str(worktree / '.git/index'),
            }

            def git(*args):
                result = subprocess.run(['git', '-c', 'core.autocrlf=true', *args],
                                        cwd=worktree, env=env, capture_output=True, text=True)
                self.assertEqual(result.returncode, 0, result.stderr)

            git('init', '-q')
            attributes = ROOT / '.gitattributes'
            (worktree / '.gitattributes').write_bytes(attributes.read_bytes() if attributes.exists() else b'')
            paths = ['docs/package-readme.md', 'LICENSE']
            for name in paths:
                target = worktree / name
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes((ROOT / name).read_bytes())
            git('add', '.gitattributes', *paths)
            export = worktree / 'export'
            git('checkout-index', '--all', f'--prefix={export}/')
            for name in paths:
                self.assertEqual((export / name).read_bytes(), (ROOT / name).read_bytes(),
                                 f'Windows checkout changes shared package payload: {name}')
