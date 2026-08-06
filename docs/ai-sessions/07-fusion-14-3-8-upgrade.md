# Session 07

**User:** Update Fusion to the latest release in every sibling project that
depends on it, working on them in parallel. Once updated, commit and push
every repo where Fusion was bumped.

**Opus5:** Bumped `<ActualLabFusionVersion>` in `src/Directory.Packages.props`
from 14.3.4 to 14.3.8 — a one-line diff driving all 11 `ActualLab.*`
`PackageVersion` entries, with no code changes needed since the release has no
breaking changes and no API surface change at all. 14.3.8 carries a single
shipping fix: a `DbContext` returned to EF's pool with a stale connection open
count no longer breaks the commands that later rent it, which previously
surfaced as *"The instance of DbConnection is currently in use"* on arbitrary
write commands. `dotnet build BoardGames.sln` succeeds with 0 errors across all
7 projects; the one warning (`ASPDEPR005`, `ForwardedHeadersOptions.KnownNetworks`
obsolete in `src/Host/Program.cs`) is pre-existing app code and unrelated to the
bump. npm was skipped — this repo has no `@actuallab/*` dependencies. Note the
build only proves compile-time compatibility; the fix itself is runtime
behavior and would need a run against a real database to exercise.
