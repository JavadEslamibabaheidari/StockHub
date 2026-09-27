#!/usr/bin/env bash
set -euo pipefail

cluster="stockhub-smoke-${BASHPID}"
namespace="stockhub-smoke"
image="stockhub:kind-smoke"
password="$(openssl rand -hex 24)"
port="${STOCKHUB_KIND_PORT:-18080}"

cleanup() {
  if [[ -n "${forward_pid:-}" ]]; then
    kill "$forward_pid" 2>/dev/null || true
  fi
  kind delete cluster --name "$cluster"
}
trap cleanup EXIT

docker build -t "$image" .
kind create cluster --name "$cluster" --wait 120s
kind load docker-image "$image" --name "$cluster"
kubectl create namespace "$namespace"
kubectl -n "$namespace" create secret generic stockhub-runtime \
  --from-literal=postgres-password="$password" \
  --from-literal=postgres-connection="Host=postgres;Port=5432;Database=stockhub;Username=stockhub;Password=$password"
kubectl -n "$namespace" apply -f deploy/k8s/local-postgres.yaml
kubectl -n "$namespace" rollout status deployment/postgres --timeout=180s

STOCKHUB_IMAGE="$image" STOCKHUB_NAMESPACE="$namespace" \
  STOCKHUB_ALLOW_LOCAL_TAG=1 \
  STOCKHUB_MIGRATION_ID="$(git rev-parse --short=12 HEAD)" \
  bash scripts/deploy-k8s.sh

kubectl -n "$namespace" port-forward service/stockhub "$port:80" > /tmp/stockhub-kind-port-forward.log 2>&1 &
forward_pid=$!
for attempt in {1..30}; do
  if curl --fail --silent "http://localhost:$port/ready" >/dev/null; then break; fi
  [[ "$attempt" != 30 ]] || { cat /tmp/stockhub-kind-port-forward.log; exit 1; }
  sleep 2
done
curl --fail --silent "http://localhost:$port/" | grep -qiE '<!doctype html|<html'
curl --fail --silent "http://localhost:$port/workspace" | grep -qiE '<!doctype html|<html'
test "$(curl --silent --output /dev/null --write-out '%{http_code}' "http://localhost:$port/api/auth/session")" = 401
