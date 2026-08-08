#!/usr/bin/env bash
set -euo pipefail

# Idempotent EC2 bootstrap, executed automatically by the CI pipeline before every
# deploy (see .github/workflows/ci-cd.yml). A brand-new EC2 instance with nothing but
# an SSH key and TCP 80 open is made production-ready here - no manual steps needed.
#
# Safe to re-run on every deploy: Docker is only installed when missing, and .env is
# only generated once. Runs from ~/onnorokom (the deploy root); sudo is used only for
# system-level changes and is passwordless on the default Ubuntu AMI.
#
#   bash deploy/ec2-provision.sh

cd "$(dirname "$0")/.."

# ---- Docker Engine + compose plugin ------------------------------------------
if ! command -v docker >/dev/null 2>&1; then
  echo "Installing Docker..."
  curl -fsSL https://get.docker.com | sudo sh
fi

sudo systemctl enable --now docker >/dev/null 2>&1 || true
docker compose version

# ---- .env (generated once, kept secret) ---------------------------------------
# Referenced by docker-compose.prod.yml. Edit afterwards to change JWT issuer,
# audience, expiry or the CORS list.
if [[ ! -f .env ]]; then
  POSTGRES_PASSWORD="$(openssl rand -base64 24 | tr -d '/+=')"
  JWT_SECRET="$(openssl rand -hex 32)"
  cat > .env <<EOF
# ---- PostgreSQL -----------------------------------------------------------------
POSTGRES_DB=assignmentmanagement
POSTGRES_USER=assignment
POSTGRES_PASSWORD=$POSTGRES_PASSWORD

# Connection string used by the API container. Host must be the compose service
# name "postgres"; keep in sync with the three POSTGRES_* values above.
DB_CONNECTION_STRING=Host=postgres;Port=5432;Database=assignmentmanagement;Username=assignment;Password=$POSTGRES_PASSWORD

# ---- JWT -------------------------------------------------------------------------
# At least 32 characters - the API refuses to start with a shorter secret.
JWT_SECRET=$JWT_SECRET
JWT_ISSUER=AssignmentManagement.Api
JWT_AUDIENCE=AssignmentManagement.Client
JWT_EXPIRES_MINUTES=60

# ---- API / CORS -------------------------------------------------------------------
# Replace with the EC2 host's public address if you call the API from a browser
# directly; the nginx frontend proxies /api same-origin, so this is informational.
CORS_ALLOWED_ORIGINS=http://$(curl -fsS http://checkip.amazonaws.com)

# ---- Ports --------------------------------------------------------------------------
FRONTEND_PORT=80
API_PORT=5105

# Run migrations and seed demo users on API startup (idempotent).
SEED_ON_STARTUP=true
EOF
  chmod 600 .env
  echo ".env generated."
else
  echo ".env already present - leaving it untouched."
fi

echo "EC2 host is provisioned."
