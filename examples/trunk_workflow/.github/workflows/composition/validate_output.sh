#!/bin/bash
set -euo pipefail

VERSION_NEXT="${VERSION_NEXT:-}"
if [ -z "$VERSION_NEXT" ]; then
  echo "::error::VERSION_NEXT is empty" >&2
  exit 1
fi

# Example validate excepted files to be release.

echo "Releasing version: $VERSION_NEXT"
OUTPUT="/tmp/artifacts/output"

echo "Checking directory: $OUTPUT"
# Define array of expected filenames
ALLOWED_FILES=(
  "_.$VERSION_NEXT.md" # Confirm the changelog does exist.
  "build.env" # confirm the exporting env variable are well exported.
)

# Fail immediately if a required file is missing
for file in "${ALLOWED_FILES[@]}"; do
  if [ ! -f "$OUTPUT/$file" ]; then
    echo "❌ Error: Required file missing -> $file"
    echo "🚨 Validation failed!"
    exit 1
  fi
done

ls "$OUTPUT"

echo "✅ Validation passed successfully! Match complete."
