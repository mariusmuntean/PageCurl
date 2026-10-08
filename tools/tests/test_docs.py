import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('check_docs', Path(__file__).parents[1] / 'check-docs.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class DocumentationTests(unittest.TestCase):
    def test_video_source_and_poster_links_must_exist(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'index.html').write_text('<video poster="missing.jpg"><source src="missing.mp4"></video>')
            self.assertEqual(len(module.check_site(root)), 2)

    def test_accepts_local_video_and_poster(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'index.html').write_text('<video controls preload="none" poster="preview.jpg"><source src="preview.mp4"></video>')
            (root / 'preview.jpg').write_bytes(b'poster fixture')
            (root / 'preview.mp4').write_bytes(b'video fixture')
            self.assertEqual(module.check_site(root), [])

    def test_accepts_relative_links_and_encoded_anchors(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'index.html').write_text('<a href="guide.html#page%20turn">Read</a>')
            (root / 'guide.html').write_text('<h1 id="page turn">Guide</h1>')
            self.assertEqual(module.check_site(root), [])

    def test_rejects_missing_pages_and_anchors(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'index.html').write_text('<a href="missing.html">Bad</a><a href="#absent">Bad</a>')
            self.assertEqual(len(module.check_site(root)), 2)

    def test_rejects_paths_outside_the_published_site(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'index.html').write_text('<a href="../private.log">Bad</a>')
            self.assertIn('outside site', module.check_site(root)[0])

    def test_does_not_publish_validation_logs(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'index.html').write_text('<h1>Docs</h1>')
            (root / 'build.log').write_text('private build output')
            self.assertIn('Unexpected publication', module.check_site(root)[0])

    def test_rejects_local_build_paths(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'index.html').write_text('<h1>Docs</h1>')
            (root / 'manifest.json').write_text('{"source_base_path": "/Users/person/private"}')
            self.assertTrue(any('Local build path' in error for error in module.check_site(root)))

    def test_rejects_unavailable_private_source_links(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'index.html').write_text('<a href="https://github.com/mariusmuntean/PageCurl/blob/main/a.cs">Source</a>')
            self.assertTrue(any('Private source link' in error for error in module.check_site(root)))
