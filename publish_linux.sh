#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

dotnet publish Convertor \
  -c Release \
  -r linux-x64 \
  --self-contained \
  -o publish \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

echo
echo "✓ Binário: $(pwd)/publish/convertor"
