#!/usr/bin/env bash
# Genuine Unity CLI+MCP on the user-controlled Linux desktop; requires Unity sign-in.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/UnityArcFlight"
echo "ARC FLIGHT project: $PROJECT"
if ! command -v unity >/dev/null 2>&1; then
  echo "Unity CLI missing. Official guide: https://docs.unity.com/en-us/unity-cli"
  exit 2
fi
unity --version
unity editors list || true
echo "You must sign in to Unity on this computer."
unity auth login
echo "If a Unity 6 Editor is not installed: unity install lts"
unity open "$PROJECT"
echo "When Editor is ready, separately run:"
echo "cd \"$PROJECT\" && unity pipeline install && unity pipeline list"
echo "Then: unity mcp configure codex && unity status"
