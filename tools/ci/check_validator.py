#!/usr/bin/env python3
"""Exit nonzero when the scenario validator report is missing or has errors."""
from __future__ import annotations

import json
import sys
from pathlib import Path


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: check_validator.py <scenario-validation.json>", file=sys.stderr)
        return 1
    path = Path(sys.argv[1])
    if not path.is_file():
        print(f"Validator report was not written: {path}", file=sys.stderr)
        return 1
    report = json.loads(path.read_text(encoding="utf-8"))
    errors = int(report.get("errorCount", 0))
    warnings = int(report.get("warningCount", 0))
    print(f"validator errors={errors} warnings={warnings}")
    if errors > 0:
        print("Scenario validator failed. The build must not ship.", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
