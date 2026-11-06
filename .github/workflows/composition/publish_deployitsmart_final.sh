#!/bin/bash
set -euo pipefail

VERSION_NEXT="${VERSION_NEXT}"
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
  "-p:Version=$VERSION_NEXT"
  "-p:InformationalVersion=$VERSION_NEXT"
  "-p:AssemblyVersion=$VERSION_NEXT"
  "-p:FileVersion=$VERSION_NEXT"
  "-p:PublishAot=false"
)

PROJECT_PATH="$GITHUB_WORKSPACE/src/DeployItSmart.csproj"

# 5. Execute commands cleanly utilizing the array syntax ("${ARRAY_NAME[@]}")
dotnet publish "$PROJECT_PATH" --self-contained true -c Release -r win-x64 -o "$WIN_OUT"  "${COMMON_ARGS[@]}"

dotnet publish "$PROJECT_PATH" --self-contained true -c Release -r linux-x64 -o "$LINUX_OUT" "${COMMON_ARGS[@]}"

dotnet pack "$PROJECT_PATH" -o "$OUT" "${COMMON_ARGS[@]}"

tar -czvf "$OUT/deployitsmart_win_x64.tar.gz" -C "$WIN_OUT" .
tar -czvf "$OUT/deployitsmart_linux_x64.tar.gz" -C "$LINUX_OUT" .
