#!/bin/bash
set -euo pipefail

VERSION="${VERSION_NEXT:-}"
VERSION_NS="${VERSION_NOSUFFIX:-}"
if [ -z "$VERSION_NOSUFFIX" ]; then
    $VERSION_NS = $VERSION # IF NOT PREVIEW SAY THE VERSION NO SUFFIX, IF PREVIEW YOU GET A x.x.x HERE
fi
MAJOR="${VERSION_NS%%.*}"
MAJOR_ZERO_ZERO="${MAJOR}.0.0" # FOR .NET PROJECT ASSEMBLY VERSION.

BRANCH="${BRANCH:-}"
echo "Releasing version: $VERSION"

# Define array of expected filenames
ALLOWED_FILES=(
  "_.$VERSION.md" # Confirm the changelog does exist.
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
