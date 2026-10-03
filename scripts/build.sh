#!/usr/bin/env bash
# Builds Blackout in a throwaway .NET 10 SDK container and deploys it to Dalamud's devPlugins folder.
set -euo pipefail

HERE="$(cd "$(dirname "$0")/.." && pwd)"
CONFIG="${CONFIG:-Release}"

# The Windows Roaming folder. Set WIN_APPDATA to override.
WIN_APPDATA="${WIN_APPDATA:-$(wslpath "$(cmd.exe /c echo %APPDATA% 2>/dev/null | tr -d '\r')")}"
DALAMUD_LIBS="$WIN_APPDATA/XIVLauncher/addon/Hooks/dev"
DEPLOY_DIR="$WIN_APPDATA/XIVLauncher/devPlugins/Blackout"

podman run --rm \
  -v "$HERE:/src" \
  -v "$DALAMUD_LIBS:/dalamud:ro" \
  -v blackout-nuget:/root/.nuget/packages \
  -e DALAMUD_HOME=/dalamud \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  -e DOTNET_NOLOGO=1 \
  -w /src/src/Blackout \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet build -c "$CONFIG" -o /src/out

mkdir -p "$DEPLOY_DIR"
cp "$HERE/out/Blackout.dll" "$HERE/out/Blackout.json" "$DEPLOY_DIR/"
echo "Deployed to: $DEPLOY_DIR"
