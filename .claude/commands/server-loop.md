---
allowed-tools: Bash, Read
description: Inspect a running server-loop.cmd/.ps1 — show current stage, last step output, server reachability
---

# /server-loop

Use this when the user has started `server-loop.cmd` (or `server-loop.ps1`)
in another terminal and wants to know where it is, why it stalled, or
whether the server is reachable.

The loop owns the BoardGames server process — don't start `dotnet run`
yourself while it's running; the two would fight over the port.

## Is the loop running at all?

The loop writes its PID to `tmp/server-loop.pid` on startup. Check it:

```powershell
Get-Process -Id (Get-Content tmp/server-loop.pid)   # pwsh; ps -p <pid> on POSIX
```

If the PID file is missing or that PID is dead, the loop is NOT running.
What to do about it depends on where YOU are running (check `AC_OS`):

- **Host OS** (`AC_OS` = `Windows`/`macOS`/`Linux`, or unset): it's fine
  for you (an AI agent) to start the loop yourself as a background task
  and keep it running while you work:

  ```powershell
  pwsh -NoProfile -File server-loop.ps1   # run in the background, don't await
  ```

- **Docker/WSL** (`AC_OS` = `Linux in Docker` / `Linux on WSL`): the PID
  check itself is unreliable — the loop runs on the host, and its PID means
  nothing in your PID namespace (probe `http://localhost:5030` for a hint
  instead). You also can't start a host process from there, so ask the
  user to run `server-loop.cmd` on the host.

## How the loop works

Two steps per iteration:

1. `dotnet-build` — `dotnet build -c <Configuration> src/Host/Host.csproj`
2. `server-run` — runs the built server binary from
   `src/Host/bin/<Configuration>/net10.0/` on `http://localhost:5030`
   (override with `-Port`).

A watchdog probes `http://localhost:5030` every 15 s (first probe 10 s
after start); two consecutive misses force-kill the server and the loop
recycles into a fresh build.

## Restarting the server (rebuild + relaunch)

Any of these works:

- **Create `tmp/server-loop.restart`** — the loop stops the server,
  deletes the marker, and starts a fresh iteration. This is the way to
  do it from another terminal, from Docker/WSL, or as an AI agent:
  `New-Item tmp/server-loop.restart` (pwsh) or `touch tmp/server-loop.restart`.
- Any keypress in the loop terminal.

A **failed step** (build error, port in use, etc.) parks the loop — the
marker line `Last step failed, remove this file to restart the loop.` is
the last line of `tmp/server-loop.log`. To unstick, delete
`tmp/server-loop.log` or press a key in the loop terminal.

### The loop rebuild is the PREFERRED way to check changes

When the loop is running, validate C#/Razor changes by triggering a loop
restart and watching its logs — not by running a separate `dotnet build`
(a second build just duplicates work and can race the loop's own build):

1. Edit code.
2. `touch tmp/server-loop.restart` (or delete `tmp/server-loop.log` if
   the loop parked at "Last step failed").
3. Watch `tmp/server-loop.log` for `Step 2/2 (server-run)` and check
   `tmp/server-loop-dotnet-build.log` if step 1 fails.
4. Reload the browser page.

## Where to look

| File | Purpose |
|------|---------|
| `tmp/server-loop.log` | Stage transitions + failure marker (the "what's happening" view) |
| `tmp/server-loop-dotnet-build.log` | Step 1 stdout/stderr (dotnet build) |
| `tmp/server-loop-server-run.out` | Step 2 — server stdout |
| `tmp/server-loop-server-run.err` | Step 2 — server stderr (empty on a healthy run) |

All files are wiped at the start of each loop iteration. The loop
banner-prints these paths once at startup and never again, so per-step
log lines stay terse — `[hh:mm:ss] Step N/2 (name)`.

## Reachability

- **`http://localhost:5030`** — the server URL (no NGINX, no TLS in this
  project). If the loop was started with a custom `-Port`, the banner at
  the top of the loop terminal (and `ASPNETCORE_URLS` in the probe
  misses in `tmp/server-loop.log`) shows the actual URL.

## Quick status check

```bash
#!/bin/bash
cd "$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
echo "=== server-loop state ==="
if [ ! -f tmp/server-loop.log ]; then
    echo "tmp/server-loop.log: NOT FOUND -> loop not running, or between iterations."
else
    tail -20 tmp/server-loop.log
    if tail -1 tmp/server-loop.log | grep -q "remove this file to restart"; then
        echo "PAUSED: delete tmp/server-loop.log (or press a key in the loop terminal) to restart."
    fi
fi
echo
echo "=== reachability ==="
curl -s -o /dev/null -w "http://localhost:5030 -> HTTP %{http_code}\n" --max-time 3 http://localhost:5030 \
    || echo "http://localhost:5030 -> DOWN"
```

## Interpreting the result

- **Loop log shows `Step 2/2 (server-run)` as the latest line and the URL
  is up:** healthy run state. Edit code, then `touch tmp/server-loop.restart`.
- **Loop log ends with `Last step failed, remove this file to restart`:**
  open `tmp/server-loop-dotnet-build.log` (or `.out`/`.err`) for the
  actual error. Fix the code, then delete `tmp/server-loop.log`.
- **URL down while loop is on step 2:** the server crashed between
  startup and now. `tmp/server-loop-server-run.err` has the details.
- **No loop log at all:** the loop is probably not running — verify via
  the PID check above, then start it yourself (host OS) or ask the user
  to start it (Docker/WSL).

## Picking up script edits

`server-loop.ps1` is loaded once when the loop starts — PowerShell does
not hot-reload. After editing the script, the host needs to **restart
the loop** for the changes to take effect. C#/Razor changes are picked
up by the next rebuild and don't need a loop restart.
