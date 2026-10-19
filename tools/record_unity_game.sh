#!/usr/bin/env bash
# Record actual Unity Editor Play-mode pixels. Never renders simulated footage.
set -euo pipefail
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT="${1:-$DIR/../recordings/unity-arc-flight-$(date +%Y%m%d-%H%M%S).mp4}"
mkdir -p "$(dirname "$OUT")"
echo "Start Unity Editor Play mode, then record to: $OUT"
if [[ -n "${WAYLAND_DISPLAY:-}" ]] && command -v wf-recorder >/dev/null; then
  echo "Wayland wlroots recorder; Ctrl+C to stop"
  exec wf-recorder -f "$OUT" -r 30
elif [[ -n "${DISPLAY:-}" ]] && command -v ffmpeg >/dev/null; then
  echo "X11 recorder; q or Ctrl+C to stop"
  exec ffmpeg -y -f x11grab -framerate 30 -i "$DISPLAY" -c:v libx264 -preset ultrafast -crf 22 -pix_fmt yuv420p "$OUT"
else
  echo "No supported screen capture method. Use OBS Studio with PipeWire portal for GNOME/KDE Wayland." >&2
  exit 3
fi
