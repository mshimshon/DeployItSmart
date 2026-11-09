#!/bin/bash
set -euo pipefail

VERSION_NEXT="${VERSION_NEXT:-}"
if [ -z "$VERSION_NEXT" ]; then
    echo "::error::VERSION_NEXT is empty" >&2
    exit 1
fi
# Anything to Produce OUTPUT VERSION
