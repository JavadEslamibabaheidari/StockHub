#!/usr/bin/env bash
set -euo pipefail

: "${STOCKHUB_IMAGE:?Set STOCKHUB_IMAGE to an immutable image digest}"
: "${STOCKHUB_NAMESPACE:?Set STOCKHUB_NAMESPACE}"
: "${STOCKHUB_MIGRATION_ID:?Set STOCKHUB_MIGRATION_ID to the source revision}"

if [[ "${STOCKHUB_ALLOW_LOCAL_TAG:-}" == "1" ]]; then
  [[ "$STOCKHUB_IMAGE" =~ ^[a-zA-Z0-9./_-]+:[a-zA-Z0-9._-]+$ ]] || exit 2
else
  [[ "$STOCKHUB_IMAGE" =~ ^[a-zA-Z0-9./_-]+@sha256:[a-f0-9]{64}$ ]] || {
    echo "STOCKHUB_IMAGE must be an image digest" >&2
    exit 2
  }
fi
[[ "$STOCKHUB_NAMESPACE" =~ ^[a-z0-9]([-a-z0-9]*[a-z0-9])?$ ]] || exit 2
[[ "$STOCKHUB_MIGRATION_ID" =~ ^[a-f0-9]{7,40}$ ]] || exit 2

job_name="stockhub-migrate-${STOCKHUB_MIGRATION_ID:0:12}"
tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

render() {
  sed \
    -e "s|__STOCKHUB_IMAGE__|$STOCKHUB_IMAGE|g" \
    -e "s|__MIGRATION_JOB__|$job_name|g" \
    "$1" > "$2"
}

kubectl -n "$STOCKHUB_NAMESPACE" get secret stockhub-runtime >/dev/null

render deploy/k8s/migration.yaml "$tmp_dir/migration.yaml"
kubectl -n "$STOCKHUB_NAMESPACE" apply -f "$tmp_dir/migration.yaml"
if ! kubectl -n "$STOCKHUB_NAMESPACE" wait --for=condition=complete "job/$job_name" --timeout=180s; then
  kubectl -n "$STOCKHUB_NAMESPACE" logs "job/$job_name" --all-containers=true || true
  exit 1
fi

render deploy/k8s/app.yaml "$tmp_dir/app.yaml"
previous_image="$(kubectl -n "$STOCKHUB_NAMESPACE" get deployment stockhub -o jsonpath='{.spec.template.spec.containers[0].image}' 2>/dev/null || true)"
rollback() {
  if [[ -n "$previous_image" ]]; then
    kubectl -n "$STOCKHUB_NAMESPACE" set image deployment/stockhub "stockhub=$previous_image"
    kubectl -n "$STOCKHUB_NAMESPACE" rollout status deployment/stockhub --timeout=180s || true
  fi
}
kubectl -n "$STOCKHUB_NAMESPACE" apply -f "$tmp_dir/app.yaml"
if ! kubectl -n "$STOCKHUB_NAMESPACE" rollout status deployment/stockhub --timeout=180s; then
  rollback
  exit 1
fi

if [[ -n "${STOCKHUB_HOST:-}" ]]; then
  : "${STOCKHUB_INGRESS_CLASS:?Set STOCKHUB_INGRESS_CLASS for shared deployments}"
  [[ "$STOCKHUB_HOST" =~ ^[a-zA-Z0-9.-]+$ ]] || exit 2
  [[ "$STOCKHUB_INGRESS_CLASS" =~ ^[a-z0-9]([-a-z0-9]*[a-z0-9])?$ ]] || exit 2
  sed \
    -e "s|__STOCKHUB_HOST__|$STOCKHUB_HOST|g" \
    -e "s|__INGRESS_CLASS__|$STOCKHUB_INGRESS_CLASS|g" \
    deploy/k8s/ingress.yaml > "$tmp_dir/ingress.yaml"
  kubectl -n "$STOCKHUB_NAMESPACE" apply -f "$tmp_dir/ingress.yaml"
fi

if [[ "${STOCKHUB_LOCAL_NODEPORT:-}" == "1" ]]; then
  kubectl -n "$STOCKHUB_NAMESPACE" apply -f deploy/k8s/local-service.yaml
fi

if [[ -n "${STOCKHUB_VERIFY_URL:-}" ]]; then
  verified=0
  for attempt in {1..12}; do
    if curl --fail --silent --show-error --max-time 10 "$STOCKHUB_VERIFY_URL/ready" >/dev/null; then
      verified=1
      break
    fi
    sleep 5
  done
  if [[ "$verified" != 1 ]]; then
    rollback
    exit 1
  fi
fi

kubectl -n "$STOCKHUB_NAMESPACE" get deployment/stockhub service/stockhub "job/$job_name"
