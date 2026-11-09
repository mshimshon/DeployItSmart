#!/bin/bash
set -euo pipefail

VERSION="${VERSION_NEXT:-}"
VERSION_NS="${VERSION_NOSUFFIX:-}"
if [ -z "$VERSION_NS" ]; then
    VERSION_NS="$VERSION"
fi
MAJOR="${VERSION_NS%%.*}"
MAJOR_ZERO_ZERO="${MAJOR}.0.0"

BRANCH="${BRANCH:-}"


echo "Releasing version: $VERSION"
OUT="/tmp/artifacts/output";
WIN_OUT="/tmp/artifacts/publish/win";
LINUX_OUT="/tmp/artifacts/publish/linux";
mkdir -p "$OUT"
mkdir -p "$WIN_OUT"
mkdir -p "$LINUX_OUT"

# 4. Define your versioning and optimization flags in an array
COMMON_ARGS=(
  "-p:DebugSymbols=false"
  "-p:DebugType=none"
  "-p:Version=$VERSION"
  "-p:InformationalVersion=$VERSION"
  "-p:AssemblyVersion=$MAJOR_ZERO_ZERO"
  "-p:FileVersion=$VERSION_NS" # Always use the next natural version for the assembly that way version changes from the currrent stable.
  "-p:PublishAot=false"
)

PROJECT_PATH="$GITHUB_WORKSPACE/src/DeployItSmart.csproj"

# 5. Execute commands cleanly utilizing the array syntax ("${ARRAY_NAME[@]}")
dotnet publish "$PROJECT_PATH" --self-contained true -c Release -r win-x64 -o "$WIN_OUT"  "${COMMON_ARGS[@]}"

dotnet publish "$PROJECT_PATH" --self-contained true -c Release -r linux-x64 -o "$LINUX_OUT" "${COMMON_ARGS[@]}"

dotnet pack "$PROJECT_PATH" -o "$OUT" "${COMMON_ARGS[@]}"

tar -czvf "$OUT/deployitsmart_win_x64.tar.gz" -C "$WIN_OUT" .
tar -czvf "$OUT/deployitsmart_linux_x64.tar.gz" -C "$LINUX_OUT" .
