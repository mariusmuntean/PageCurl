from pathlib import Path
import os
import shutil
import subprocess
import tempfile
import unittest

CHECKER = Path(__file__).resolve().parents[1] / "check-workflow-projects.sh"


class WorkflowProjectTests(unittest.TestCase):
    def check(self, command, portable=False):
        with tempfile.TemporaryDirectory(prefix="pagecurl-workflow-") as folder:
            workflow = Path(folder) / "verify.yml"
            workflow.write_text(f"steps:\n  - run: {command}\n")
            env = os.environ.copy()
            if portable:
                tools = Path(folder) / "bin"
                tools.mkdir()
                for name in ("dirname", "grep", "sort"):
                    (tools / name).symlink_to(shutil.which(name))
                env["PATH"] = str(tools)
            return subprocess.run(["/bin/bash", str(CHECKER), str(workflow)],
                                  env=env, capture_output=True, text=True)

    def test_existing_test_project_is_checked(self):
        result = self.check("dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("OK tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj", result.stdout)

    def test_checker_requires_only_standard_unix_tools(self):
        result = self.check("dotnet build tests/MissingValidationGate/Missing.csproj", portable=True)
        self.assertEqual(1, result.returncode, result.stderr)
        self.assertIn("MISSING tests/MissingValidationGate/Missing.csproj", result.stdout)

    def test_missing_test_project_fails(self):
        result = self.check("dotnet build tests/MissingValidationGate/Missing.csproj")
        self.assertEqual(1, result.returncode)
        self.assertIn("MISSING tests/MissingValidationGate/Missing.csproj", result.stdout)


if __name__ == "__main__":
    unittest.main()
