#!/bin/bash
set -euo pipefail

rm -rf /tmp/deploy_scripting
git clone --branch main "https://github.com/mshimshon/deploy_scripting.git" "/tmp/deploy_scripting"
cp -Rf "$GITHUB_WORKSPACE"/* /tmp/deploy_scripting/
chmod -R +x /tmp/deploy_scripting/
dotnet restore
dotnet build -c Release
dotnet build /tmp/deploy_scripting/src/DeployItSmart.csproj -c Release
