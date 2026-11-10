#!/usr/bin/env bash
set -euo pipefail
if [[ $# -ne 0 || "${SAVEPOINT_POSTGRES_TESTS:-}" != "1" ]]; then
  echo 'Run SAVEPOINT_POSTGRES_TESTS=1 bash scripts/test-postgres.sh (no arguments).' >&2
  exit 2
fi
exec python3 -B "$(dirname -- "${BASH_SOURCE[0]}")/test_postgres.py"
