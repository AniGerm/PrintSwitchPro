#!/usr/bin/env bash
set -euo pipefail
python3 -m pip install --user pre-commit
export PATH="$HOME/.local/bin:$PATH"
pre-commit install
echo "pre-commit installiert. Test: pre-commit run --all-files"
