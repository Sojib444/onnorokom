#!/usr/bin/env bash
set -euo pipefail

# One-time bootstrap for an Ubuntu EC2 host that will run the production stack.
#
#   sudo bash deploy/ec2-setup.sh
#
# What it does:
#   1. Installs Docker + the compose plugin.
#   2. Creates /opt/onnorokom (where docker-compose.prod.yml and .env live).
#   3. Generates a strong .env (random Postgres password + JWT secret) if absent.
#
# Afterwards the CI pipeline (push to main) deploys automatically. To deploy manually:
#   cd /opt/onnorokom
#   docker compose -f docker-compose.prod.yml up -d

APP_DIR=/opt/onnorokom

if [[ $EUID -ne 0 ]]; then
  echo "Please run with sudo/root." >&2
  exit 1
fi

# The user that runs the setup (via sudo) is the one that deploys later: the pipeline
# SCPs files into $APP_DIR and runs docker compose, so that user needs write access to
# the app directory and membership in the docker group (effective on the next SSH login).
APP_OWNER="${SUDO_USER:-$(logname 2>/dev/null || echo root)}"

# ---- Docker + compose plugin -------------------------------------------------
if ! command -v docker >/dev/null 2>&1; then
  echo "Installing Docker..."
  curl -fsSL https://get.docker.com | sh
fi

systemctl enable --now docker
systemctl enable --now containerd
docker compose version

if [[ "$APP_OWNER" != "root" ]]; then
  usermod -aG docker "$APP_OWNER"
fi

# ---- App directory ------------------------------------------------------------
# /opt is root-owned, so hand the app directory to the deploying user - otherwise the
# CI pipeline cannot SCP docker-compose.prod.yml into it.
mkdir -p "$APP_DIR"
mkdir -p "$APP_DIR/uploads"
chown -R "$APP_OWNER:$APP_OWNER" "$APP_DIR"
chmod 755 "$APP_DIR"

# ---- .env ----------------------------------------------------------------------
# Generated once, kept secret (chmod 600), and referenced by docker-compose.prod.yml.
# Edit it afterwards if you want a different JWT issuer/audience, expiry, or CORS list.
if [[ ! -f "$APP_DIR/.env" ]]; then
  POSTGRES_PASSWORD="$(openssl rand -base64 24 | tr -d '/+=')"
  JWT_SECRET="$(openssl rand -hex 32)"
  cat > "$APP_DIR/.env" <<EOF
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
  chmod 600 "$APP_DIR/.env"
  echo ".env generated at $APP_DIR/.env - review it before the first deploy."
else
  echo ".env already exists at $APP_DIR/.env - leaving it untouched."
fi

echo
echo "EC2 host ready. Next steps:"
echo "  1. Copy docker-compose.prod.yml to $APP_DIR (CI does this automatically)."
echo "  2. If the GitHub repo is private, create a fine-grained PAT with 'read:packages'"
echo "     and store GHCR_USER + GHCR_TOKEN in $APP_DIR/.env for the image pull."
echo "  3. Open the security group for TCP 80 (and SSH 22)."
echo "  4. Configure the GitHub Actions secrets listed in the README, then push to main."
