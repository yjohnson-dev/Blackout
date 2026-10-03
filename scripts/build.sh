#!/usr/bin/env bash
# Builds Blackout in a container and, when a target is known, copies it to the Dalamud dev-plugins folder.
#
# Overrides:
#   DALAMUD_LIBS       Dalamud development libraries (compile-time references).
#   DEVPLUGINS_DIR     Where to copy the plugin. The deploy step is skipped when this is empty.
#   CONTAINER_RUNTIME  podman or docker. Defaults to whichever is on the PATH.
#   CONFIG             Build configuration. Defaults to Release.
set -euo pipefail

HERE="$(cd "$(dirname "$0")/.." && pwd)"
CONFIG="${CONFIG:-Release}"

# %APPDATA% as a Linux path, when this runs inside WSL.
windows_appdata() {
    if command -v cmd.exe >/dev/null 2>&1 && command -v wslpath >/dev/null 2>&1; then
        wslpath "$(cmd.exe /c echo %APPDATA% 2>/dev/null | tr -d '\r')"
    fi
}

if [ -z "${DALAMUD_LIBS:-}" ]; then
    if [ -n "${DALAMUD_HOME:-}" ]; then
        DALAMUD_LIBS="$DALAMUD_HOME"
    elif win="$(windows_appdata)" && [ -n "$win" ]; then
        DALAMUD_LIBS="$win/XIVLauncher/addon/Hooks/dev"
        [ -n "${DEVPLUGINS_DIR+set}" ] || DEVPLUGINS_DIR="$win/XIVLauncher/devPlugins/Blackout"
    elif [ -d "$HOME/.xlcore/dalamud/Hooks/dev" ]; then
        DALAMUD_LIBS="$HOME/.xlcore/dalamud/Hooks/dev"
    elif [ -d "$HOME/Library/Application Support/XIV on Mac/dalamud/Hooks/dev" ]; then
        DALAMUD_LIBS="$HOME/Library/Application Support/XIV on Mac/dalamud/Hooks/dev"
    else
        echo "error: cannot find the Dalamud libraries. Set DALAMUD_LIBS." >&2
        exit 1
    fi
fi

RUNTIME="${CONTAINER_RUNTIME:-}"
[ -n "$RUNTIME" ] || RUNTIME="$(command -v podman || command -v docker || true)"
[ -n "$RUNTIME" ] || { echo "error: install podman or docker, or set CONTAINER_RUNTIME." >&2; exit 1; }

"$RUNTIME" run --rm \
    -v "$HERE:/src" \
    -v "$DALAMUD_LIBS:/dalamud:ro" \
    -v blackout-nuget:/root/.nuget/packages \
    -e DALAMUD_HOME=/dalamud \
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    -e DOTNET_NOLOGO=1 \
    -w /src/src/Blackout \
    mcr.microsoft.com/dotnet/sdk:10.0 \
    dotnet build -c "$CONFIG" -o /src/out

if [ -n "${DEVPLUGINS_DIR:-}" ]; then
    mkdir -p "$DEVPLUGINS_DIR"
    cp "$HERE/out/Blackout.dll" "$HERE/out/Blackout.json" "$DEVPLUGINS_DIR/"
    echo "Deployed to: $DEVPLUGINS_DIR"
else
    echo "Built to: $HERE/out"
    echo "Set DEVPLUGINS_DIR to deploy automatically."
fi
