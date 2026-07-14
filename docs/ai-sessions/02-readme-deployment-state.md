# Session 02

**User:** Update the README to reflect the current state of deployment — mention it's deployed to a free-tier VM in Oracle Cloud, together with the other Fusion samples.

**Opus4.8:** Replaced the stale Google Cloud GKE / Cloud SQL section in `README.md` with the current setup: a single free-tier Arm VM in Oracle Cloud ("Always Free" A1) shared with the other Fusion samples (TownHall, TodoApp, Blazor samples) behind one Caddy edge with Cloudflare in front, PostgreSQL in Docker with host-persisted data, and systemd poll-based auto-deploy — and pointed the links at `deploy/`.
