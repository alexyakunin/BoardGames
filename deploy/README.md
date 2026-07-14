# Deployment

Single-host deployment of BoardGames behind Cloudflare, used for
`https://boardgames.actuallab.net`.

TLS termination + routing are handled by the **shared edge Caddy**, which lives
in [ActualLab/DeployConfigs](https://github.com/ActualLab/DeployConfigs)
(`oracle-vm1/edge`) and owns ports 80/443 on the VM. This stack no longer runs
its own Caddy; the app just joins the shared `edge` network and is reached by its
container name (`boardgames-app`).

## Topology

```
Browser ──HTTPS──> Cloudflare (proxied, Full strict)
                        │  HTTPS (Origin CA cert)
                        ▼
                 edge-caddy :443  ──HTTP──> boardgames-app :8080 ──> boardgames-db :5432
                 (shared edge)              (this stack, on the `edge` + `internal` nets)
```

- **edge-caddy** (in DeployConfigs) terminates TLS with the Cloudflare wildcard
  **Origin CA** certificate and reverse-proxies `boardgames.actuallab.net` to
  `boardgames-app:8080`, passing WebSockets through for Fusion RPC + Blazor Server.
- **app** (`boardgames-app`) is `BoardGames.Host`, built from the repo `Dockerfile`
  (`app_release`). It joins the shared `edge` network and a stack-private
  `internal` network.
- **db** (`boardgames-db`) is PostgreSQL on the `internal` network only. Its data
  is persisted on the host in `/var/lib/boardgames/postgres`, so it survives
  redeploys. The app applies EF Core migrations (`src/Migrations/Migrations`) on
  startup.

## First-time host setup

The shared `edge` network + edge Caddy must exist first (see DeployConfigs
`oracle-vm1`). Then:

```bash
sudo mkdir -p /opt/apps && sudo chown $USER /opt/apps
git clone https://github.com/alexyakunin/BoardGames /opt/apps/boardgames
sudo mkdir -p /var/lib/boardgames/postgres   # persistent Postgres data dir
cd /opt/apps/boardgames/deploy
cp .env.example .env            # edit secrets as needed
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
- Origin certificate: the wildcard `*.actuallab.net` Origin cert is host-managed
  in the shared edge (`DeployConfigs/oracle-vm1/edge/certs`), not here.

## Notes

- The `db` container publishes no ports and sits on the stack-internal network,
  so it's reachable only from the VM. To open a psql shell, SSH to the host and
  run: `docker exec -it boardgames-db psql -U postgres boardgames`.
- OAuth sign-in requires the GitHub/Microsoft OAuth apps to whitelist
  `https://boardgames.actuallab.net/signin-github` and `/signin-microsoft`.
  Browsing works without this; only sign-in redirects need it.
