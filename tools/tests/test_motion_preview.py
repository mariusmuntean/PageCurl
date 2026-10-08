from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


CHECKER = Path(__file__).parents[1] / 'check-motion-preview.py'


@unittest.skipUnless(shutil.which('ffmpeg') and shutil.which('ffprobe'), 'Media checks need ffmpeg and ffprobe')
class MotionPreviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.folder = tempfile.TemporaryDirectory()
        cls.addClassCleanup(cls.folder.cleanup)
        cls.root = Path(cls.folder.name)
        for name, source, duration in (
            ('moving', 'testsrc2=size=64x36:rate=30', '20'),
            ('frozen', 'color=c=white:size=64x36:rate=30', '20'),
            ('partial', 'testsrc2=size=64x36:rate=30', '1'),
        ):
            subprocess.run(['ffmpeg', '-v', 'error', '-f', 'lavfi', '-i', source,
                            '-t', duration, '-c:v', 'libx264', '-threads', '1', '-pix_fmt', 'yuv420p',
                            str(cls.root / f'{name}.mp4')], check=True, timeout=30)
        subprocess.run(['ffmpeg', '-v', 'error', '-i', str(cls.root / 'moving.mp4'),
                        '-f', 'lavfi', '-i', 'sine=frequency=440', '-t', '20',
                        '-c:v', 'copy', '-c:a', 'aac', str(cls.root / 'audio.mp4')], check=True, timeout=30)

    def check(self, name):
        return subprocess.run([sys.executable, str(CHECKER), str(self.root / f'{name}.mp4')],
                              capture_output=True, text=True, timeout=30)

    def test_accepts_complete_silent_motion(self):
        result = self.check('moving')
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_rejects_full_length_frozen_recording(self):
        result = self.check('frozen')
        self.assertNotEqual(result.returncode, 0, 'A frozen recording was accepted')
        self.assertIn('motion', result.stderr.lower())

    def test_rejects_partial_recording(self):
        result = self.check('partial')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Incomplete', result.stderr)

    def test_rejects_recorded_audio(self):
        result = self.check('audio')
        self.assertNotEqual(result.returncode, 0, 'Audio was accepted in a silent preview')
        self.assertIn('silent', result.stderr.lower())

    def test_rejects_corrupt_media(self):
        (self.root / 'corrupt.mp4').write_bytes(b'not a movie')
        result = self.check('corrupt')
        self.assertNotEqual(result.returncode, 0)
