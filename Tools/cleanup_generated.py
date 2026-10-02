#!/usr/bin/env python3
"""Preview or finish untracking historical Unity/IDE caches. Keeps local working files intact."""
import argparse
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PREFIXES = ("Library/", "Logs/", "UserSettings/", ".vs/", "obj/")
SUFFIXES = (".csproj", ".sln", ".userprefs", ".apk", ".aab")

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--apply", action="store_true", help="Stage removal from Git; do not delete local caches.")
args = parser.parse_args()
tracked = subprocess.check_output(["git", "ls-files", "-z"], cwd=ROOT).decode().split("\0")
files = [name for name in tracked if name and (name.startswith(PREFIXES) or ("/" not in name and name.endswith(SUFFIXES)))]
print(f"{len(files)} historical generated files to untrack. Assets, Packages, ProjectSettings, .git, and source tooling are not touched.")
if not args.apply:
    print("Dry run only. Run with --apply to stage the remaining cleanup; local files remain available to Unity.")
else:
    for start in range(0, len(files), 100):
        subprocess.run(["git", "rm", "--cached", "--ignore-unmatch", "--quiet", "--", *files[start:start + 100]], cwd=ROOT, check=True)
    print("Cleanup staged. Review git status before committing. This is not a history rewrite.")
