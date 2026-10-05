from pathlib import Path
import subprocess
import tempfile
import unittest

CHECKER = Path(__file__).resolve().parents[1] / "check-workflow-projects.sh"


class WorkflowProjectTests(unittest.TestCase):
    def check(self, command):
        with tempfile.TemporaryDirectory(prefix="pagecurl-workflow-") as folder:
            workflow = Path(folder) / "verify.yml"
            workflow.write_text(f"steps:\n  - run: {command}\n")
            return subprocess.run(["bash", str(CHECKER), str(workflow)], capture_output=True, text=True)

    def test_existing_test_project_is_checked(self):
        result = self.check("dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("OK tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj", result.stdout)

    def test_missing_test_project_fails(self):
        result = self.check("dotnet build tests/MissingValidationGate/Missing.csproj")
        self.assertEqual(1, result.returncode)
        self.assertIn("MISSING tests/MissingValidationGate/Missing.csproj", result.stdout)


if __name__ == "__main__":
    unittest.main()
