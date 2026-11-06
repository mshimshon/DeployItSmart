# Enable fail-fast behavior
# -e: Exit immediately if any command exits with a non-zero status
# -u: Treat unset variables as an error
# -o pipefail: Prevent errors in a pipeline from being masked
set -euo pipefail

if [ "$BRANCH" = "main-dev" ]; then
  if [ -z "$VERSION_NEXT_PREVIEW" ]; then
    echo "::error::main-dev build but VERSION_NEXT_PREVIEW is empty" >&2
    exit 1
  fi
  VERSION="$VERSION_NEXT_PREVIEW"
else
  if [ -z "$VERSION_NEXT" ]; then
    echo "::error::VERSION_NEXT is empty" >&2
    exit 1
  fi
  VERSION="$VERSION_NEXT"
fi

echo "Releasing version: $VERSION"
OUTPUT="/tmp/artifacts/output"

echo "Checking directory: $OUTPUT"

# Define array of expected filenames
ALLOWED_FILES=(
  "deployitsmart_win_x64.tar.gz"
  "deployitsmart_linux_x64.tar.gz"
  "DeployItSmart.$VERSION.nupkg"
  "_.$VERSION.md"
)

# Fail immediately if a required file is missing
for file in "${ALLOWED_FILES[@]}"; do
  if [ ! -f "$OUTPUT/$file" ]; then
    echo "❌ Error: Required file missing -> $file"
    echo "🚨 Validation failed!"
    exit 1
  fi
done

# Optional: List contents only if everything up to this point passed
ls "$OUTPUT"

echo "✅ Validation passed successfully! Match complete."
