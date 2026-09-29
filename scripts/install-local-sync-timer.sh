#!/usr/bin/env bash
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unit_dir="$HOME/.config/systemd/user"
mkdir -p "$unit_dir"
sed "s|@REPO@|$repo|g" "$repo/deploy/systemd/stockhub-local-sync.service.in" \
  > "$unit_dir/stockhub-local-sync.service"
cp "$repo/deploy/systemd/stockhub-local-sync.timer" "$unit_dir/stockhub-local-sync.timer"
systemctl --user daemon-reload
systemctl --user enable --now stockhub-local-sync.timer
systemctl --user list-timers stockhub-local-sync.timer
