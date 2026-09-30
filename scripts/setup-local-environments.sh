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
    [[ "$(stat -c %a "$path")" == 600 ]] || { echo "$path must have mode 600" >&2; exit 1; }
    if ! grep -q '^PUBLIC_BASE_URL=' "$path"; then
      printf 'PUBLIC_BASE_URL=http://127.0.0.1:%s\n' "$port" >> "$path"
    fi
    for setting in GOOGLE_CLIENT_ID= GOOGLE_CLIENT_SECRET= SMTP_HOST= SMTP_PORT=587 SMTP_FROM= SMTP_USERNAME= SMTP_PASSWORD= SMTP_USE_TLS=true; do
      if ! grep -q "^${setting%%=*}=" "$path"; then
        printf '%s\n' "$setting" >> "$path"
      fi
    done
    echo "Updated optional settings in $path"
    continue
  fi
  password="$(openssl rand -hex 32)"
  printf 'POSTGRES_PASSWORD=%s\nSTOCKHUB_PORT=%s\nPUBLIC_BASE_URL=http://127.0.0.1:%s\nASPNETCORE_ENVIRONMENT=Production\nAUTHENTICATION_SECURE_COOKIES=false\nGOOGLE_CLIENT_ID=\nGOOGLE_CLIENT_SECRET=\nSMTP_HOST=\nSMTP_PORT=587\nSMTP_FROM=\nSMTP_USERNAME=\nSMTP_PASSWORD=\nSMTP_USE_TLS=true\n' "$password" "$port" "$port" > "$path"
  chmod 600 "$path"
  echo "Created $path for localhost:$port"
done
