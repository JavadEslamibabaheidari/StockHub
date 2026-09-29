#!/usr/bin/env bash
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
config_dir="${STOCKHUB_CONFIG_DIR:-$HOME/.config/stockhub}"
umask 077
mkdir -p "$config_dir"
for pair in dev:8081 staging:8082 prod:8083; do
  target="${pair%%:*}"
  port="${pair##*:}"
  path="$config_dir/$target.env"
  if [[ -e "$path" ]]; then
    echo "Keeping existing $path"
    continue
  fi
  password="$(openssl rand -hex 32)"
  printf 'POSTGRES_PASSWORD=%s\nSTOCKHUB_PORT=%s\nASPNETCORE_ENVIRONMENT=Production\nAUTHENTICATION_SECURE_COOKIES=false\n' "$password" "$port" > "$path"
  chmod 600 "$path"
  echo "Created $path for localhost:$port"
done
