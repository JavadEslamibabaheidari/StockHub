#!/usr/bin/env bash
set -euo pipefail

cluster=stockhub-local
namespace=stockhub-local
context="kind-$cluster"

if ! kind get clusters | rg -qx "$cluster"; then
  kind create cluster --name "$cluster" --config deploy/kind-local.yaml --wait 120s
fi
kubectl --context "$context" get namespace "$namespace" >/dev/null 2>&1 || \
  kubectl --context "$context" create namespace "$namespace"

if ! kubectl --context "$context" -n "$namespace" get secret stockhub-runtime >/dev/null 2>&1; then
  password="$(openssl rand -hex 24)"
  kubectl --context "$context" -n "$namespace" create secret generic stockhub-runtime \
    --from-literal=postgres-password="$password" \
    --from-literal=postgres-connection="Host=postgres;Port=5432;Database=stockhub;Username=stockhub;Password=$password"
fi

kubectl --context "$context" -n "$namespace" apply -f deploy/k8s/local-persistent-postgres.yaml
kubectl --context "$context" -n "$namespace" rollout status deployment/postgres --timeout=180s
echo "Local Kubernetes database is ready in $context/$namespace"
