from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

RUNNER = Path(__file__).resolve().parents[1] / "run-native-fixture.py"


class NativeRunnerTests(unittest.TestCase):
    def run_fixture(self, body, previous=None, timeout=5):
        with tempfile.TemporaryDirectory(prefix="pagecurl-runner-") as folder:
            fixture = Path(folder) / "fixture"
            fixture.write_text("#!/usr/bin/env python3\nimport os, pathlib, time\n" + body)
            fixture.chmod(0o755)
            output = Path(folder) / "assertions.log"
            if previous is not None:
                output.write_text(previous)
            result = subprocess.run(
                [sys.executable, str(RUNNER), str(fixture), "--output", str(output), "--timeout", str(timeout)],
                capture_output=True, text=True, timeout=10)
            return result, output.read_text() if output.exists() else None

    def test_timeout_must_be_finite_and_positive(self):
        for value in ("nan", "inf", "0", "-1"):
            with self.subTest(value=value):
                result = subprocess.run(
                    [sys.executable, str(RUNNER), "missing", "--timeout", value],
                    capture_output=True, text=True)
                self.assertEqual(2, result.returncode)
                self.assertIn("finite and positive", result.stderr)

    def test_missing_executable_reports_controlled_failure(self):
        with tempfile.TemporaryDirectory(prefix="pagecurl-runner-") as folder:
            result = subprocess.run(
                [sys.executable, str(RUNNER), str(Path(folder) / "missing")],
                capture_output=True, text=True)
            self.assertEqual(1, result.returncode)
            self.assertIn("FAIL native fixture executable", result.stderr)
            self.assertNotIn("Traceback", result.stderr)

    def test_complete_assertions_succeed(self):
        result, log = self.run_fixture(
            "pathlib.Path(os.environ['PAGECURL_TEST_OUTPUT']).write_text('PASS snapshot\\nPASS all native suites\\n')\n")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("PASS all native suites", log)

    def test_partial_passing_output_does_not_certify_completion(self):
        result, _ = self.run_fixture(
            "pathlib.Path(os.environ['PAGECURL_TEST_OUTPUT']).write_text('PASS snapshot\\n')\n")
        self.assertEqual(1, result.returncode)
        self.assertIn("did not complete all suites", result.stderr)

    def test_failure_record_overrides_a_zero_exit_code(self):
        result, _ = self.run_fixture(
            "pathlib.Path(os.environ['PAGECURL_TEST_OUTPUT']).write_text('FAIL snapshot\\nPASS all native suites\\n')\n")
        self.assertEqual(1, result.returncode)

    def test_stale_pass_is_removed_before_a_missing_record(self):
        result, log = self.run_fixture("pass\n", previous="PASS all native suites\n")
        self.assertEqual(1, result.returncode)
        self.assertIsNone(log)
        self.assertIn("no assertion record", result.stderr)

    def test_timeout_fails_and_removes_stale_evidence(self):
        result, log = self.run_fixture("time.sleep(10)\n", previous="PASS all native suites\n", timeout=.1)
        self.assertEqual(1, result.returncode)
        self.assertIsNone(log)
        self.assertIn("exceeded", result.stderr)


if __name__ == "__main__":
    unittest.main()
