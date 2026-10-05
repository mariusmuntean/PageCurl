#!/usr/bin/env python3
"""Run the built native assertion host with a bounded timeout and retained logs."""
import argparse
import math
import os
from pathlib import Path
import subprocess
import sys

parser = argparse.ArgumentParser()
parser.add_argument("binary", type=Path)
parser.add_argument("--output", type=Path, default=Path(".artifacts/native-tests.log"))
parser.add_argument("--timeout", type=float, default=120)
args = parser.parse_args()
if not math.isfinite(args.timeout) or args.timeout <= 0:
    parser.error("--timeout must be finite and positive")
output = args.output.resolve()
output.parent.mkdir(parents=True, exist_ok=True)
# A stale passing record must never survive a failed or timed-out run.
output.unlink(missing_ok=True)
env = dict(os.environ, PAGECURL_TEST_OUTPUT=str(output))
try:
    run = subprocess.run([str(args.binary.resolve())], env=env,
                         capture_output=True, text=True, timeout=args.timeout)
    if run.stdout:
        print(run.stdout, end="")
    if run.stderr:
        print(run.stderr, end="", file=sys.stderr)
except OSError as error:
    print(f"FAIL native fixture executable: {error}", file=sys.stderr)
    sys.exit(1)
except subprocess.TimeoutExpired:
    print(f"FAIL native fixture exceeded {args.timeout:g}s", file=sys.stderr)
    sys.exit(1)
if not output.is_file():
    print("FAIL native fixture produced no assertion record", file=sys.stderr)
    sys.exit(1)
results = output.read_text()
print(results, end="")
if run.returncode or any(line.startswith("FAIL") for line in results.splitlines()):
    sys.exit(1)
if "PASS all native suites" not in results.splitlines():
    print("FAIL native fixture did not complete all suites", file=sys.stderr)
    sys.exit(1)
