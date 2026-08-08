#!/usr/bin/env bash
set -euo pipefail

# Remote deploy step, executed on the EC2 host by .github/workflows/ci-cd.yml (and
# usable manually). Pulls the newest images from GHCR and brings the stack up.
#
# Expects to run from /opt/onnorokom, next to docker-compose.prod.yml and .env.
#
#   cd /opt/onnorokom && bash deploy/ec2-update.sh

cd "$(dirname "$0")"

# Optional GHCR auth - only needed when the images are in a PRIVATE repository.
# Set GHCR_USER and GHCR_TOKEN (fine-grained PAT with read:packages) in /opt/onnorokom/.env.
if [[ -n "${GHCR_USER:-}" && -n "${GHCR_TOKEN:-}" ]]; then
  echo "Logging in to GitHub Container Registry..."
  echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USER" --password-stdin
else
  echo "No GHCR_USER/GHCR_TOKEN in .env - assuming public images."
fi

echo "Pulling latest images..."
docker compose -f docker-compose.prod.yml pull

echo "Bringing the stack up..."
docker compose -f docker-compose.prod.yml up -d --remove-orphans

echo "Waiting for the API health check..."
for _ in $(seq 1 30); do
  if curl -fsS "http://127.0.0.1:${API_PORT:-5105}/health" >/dev/null 2>&1; then
    echo "API is healthy."
    docker compose -f docker-compose.prod.yml ps
    exit 0
  fi
  sleep 2
done

echo "API health check did not pass - dumping logs:" >&2
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs --tail=60 api
exit 1
