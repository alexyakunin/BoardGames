# Deployment

Single-host deployment of BoardGames behind Cloudflare, used for
`https://boardgames.actuallab.net`.

## Topology

```
Browser ──HTTPS──> Cloudflare (proxied, Full strict)
                        │  HTTPS (Origin CA cert)
                        ▼
                   Caddy :443  ──HTTP──> app :8080 ──> Postgres :5432
                   (Docker Compose, all on one VM)
```

- **Caddy** terminates TLS with a Cloudflare **Origin CA** certificate
  (`certs/origin.pem` + `certs/origin.key`, not committed) and reverse-proxies
  to the app, passing WebSockets through for Fusion RPC + Blazor Server.
- **app** is `BoardGames.Host`, built from the repo `Dockerfile` (`app_release`).
- **db** is PostgreSQL with a persistent volume.

## First-time host setup

```bash
sudo mkdir -p /opt/apps && sudo chown $USER /opt/apps
git clone https://github.com/alexyakunin/BoardGames /opt/apps/boardgames
cd /opt/apps/boardgames/deploy
cp .env.example .env            # edit secrets as needed
mkdir -p certs                  # drop origin.pem + origin.key here
docker compose -f docker-compose.prod.yml up -d --build
```

## Auto-deploy on push

A systemd timer polls `origin/main` every minute and redeploys when it moves
(rebuild + `up -d`). No GitHub secrets or inbound webhooks required.

```bash
sudo cp systemd/boardgames-deploy.* /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now boardgames-deploy.timer
```

Force a deploy immediately: `deploy/deploy.sh --force`.

## Cloudflare

- DNS: `boardgames` A record → VM public IP, **proxied**.
- SSL/TLS mode for the host: **Full (strict)**.
- Origin certificate: generated in Cloudflare (SSL/TLS → Origin Server →
  Create Certificate), installed on the host as `certs/origin.pem` / `origin.key`.

## Notes

- OAuth sign-in requires the GitHub/Microsoft OAuth apps to whitelist
  `https://boardgames.actuallab.net/signin-github` and `/signin-microsoft`.
  Browsing works without this; only sign-in redirects need it.
