#!/usr/bin/env python3
"""Build a CI summary from Unity test XML and the scenario validator report.

Missing inputs stay null. This script does not invent pass counts or durations.
"""
from __future__ import annotations

import argparse
import json
import os
import sys
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path


def parse_tests(paths: list[str]) -> dict | None:
    files = []
    for raw in paths:
        path = Path(raw)
        if path.is_file() and path.suffix.lower() == ".xml":
            files.append(path)
        elif path.is_dir():
            files.extend(sorted(path.rglob("*.xml")))
    if not files:
        return None

    passed = 0
    failed = 0
    skipped = 0
    duration = 0.0
    saw_run = False
    for file in files:
        try:
            root = ET.parse(file).getroot()
        except ET.ParseError as exc:
            print(f"Could not parse {file}: {exc}", file=sys.stderr)
            continue
        if root.tag != "test-run":
            continue
        saw_run = True
        passed += int(root.attrib.get("passed", "0"))
        failed += int(root.attrib.get("failed", "0"))
        skipped += int(root.attrib.get("skipped", "0"))
        duration += float(root.attrib.get("duration", "0"))
    if not saw_run:
        return None
    return {
        "passed": passed,
        "failed": failed,
        "skipped": skipped,
        "durationSeconds": round(duration, 3),
    }


def parse_validator(path: str | None) -> dict | None:
    if not path:
        return None
    file = Path(path)
    if not file.is_file():
        return None
    data = json.loads(file.read_text(encoding="utf-8"))
    if "errorCount" not in data or "warningCount" not in data:
        return None
    return {"errors": int(data["errorCount"]), "warnings": int(data["warningCount"])}


def result_for(tests: dict | None, validator: dict | None) -> str:
    if tests is not None and tests["failed"] > 0:
        return "failed"
    if validator is not None and validator["errors"] > 0:
        return "failed"
    if tests is None or validator is None:
        return "incomplete"
    return "passed"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--tests", nargs="*", default=[])
    parser.add_argument("--validator", default="")
    parser.add_argument("--sha", default=os.environ.get("GITHUB_SHA", ""))
    parser.add_argument("--out", required=True)
    parser.add_argument("--history", default="")
    args = parser.parse_args()

    tests = parse_tests(args.tests)
    validator = parse_validator(args.validator or None)
    summary = {
        "commitSha": args.sha or None,
        "recordedAtUtc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "tests": tests,
        "validator": validator,
        "result": result_for(tests, validator),
    }

    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    if args.history:
        history_path = Path(args.history)
        history = []
        if history_path.is_file():
            loaded = json.loads(history_path.read_text(encoding="utf-8"))
            if isinstance(loaded, list):
                history = loaded
        history.append(summary)
        history_path.parent.mkdir(parents=True, exist_ok=True)
        history_path.write_text(json.dumps(history, indent=2) + "\n", encoding="utf-8")

    print(json.dumps(summary, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
