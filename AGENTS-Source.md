# Project-specific Rules for BoardGames

**YOU MUST READ [CODING_STYLE.md](CODING_STYLE.md) before writing or
modifying any C# code.** It's not optional. This project
**deviates from standard .NET conventions** on several points (notably:
no `Async` suffix on async methods; no XML docs on members; mixed brace
style). Default instincts from elsewhere will produce code that gets
rejected. If you haven't opened that file yet in this session, stop and
read it now.

**You MUST NOT write a single comment, docstring, or XML doc** without
first reading [CODING_STYLE.md → "Regular comments, docstrings, XML
documentation comments"](CODING_STYLE.md#regular-comments-docstrings-xml-documentation-comments).

This is an [ActualLab.Fusion](https://github.com/ActualLab/Fusion) app, so
its conventions apply here as well.

# Database & migrations

The app is PostgreSQL-only. The EF Core model (entities + `AppDbContext`)
lives in `src/HostServices`; migrations live in `src/Migrations` and are
applied by `BoardGames.Host` on startup.

**Any change to the DB model MUST come with a matching migration** in the
same commit:

```bash
dotnet ef migrations add <ChangeName> --project src/Migrations
```

Local Postgres comes from the root `docker-compose.yml`
(`docker compose up -d db`); the app and the tests expect it on
`localhost:5432` with `postgres`/`postgres` credentials.

# AI session logs

Every conversation with an AI agent in this repo must be logged to the
current session file in [docs/ai-sessions/](docs/ai-sessions/). Session files are named
`NN-description.md` (e.g. `01-init.md`); the current one is the file
with the highest `NN`.

**One session file per commit.** A session file accumulates exchanges
until they are committed:

- When you start and the latest session file has no uncommitted changes
  (per `git status`), the previous session is done — create a new file
  with the next `NN`, initially named just `NN.md` (e.g. `03.md`).
- Right before committing, rename it to add a short kebab-case
  description suffix reflecting what was done (e.g. `03.md` →
  `03-basic-app.md`).
- If the latest session file has uncommitted changes, the commit hasn't
  happened yet — keep appending to that file.
- When in doubt about committing, or about appending vs. starting a new
  session file, ask the user what to do with the session log.

For every exchange, append:

- The user's message as `**User:**` ... — but clean it up first: fix
  grammar, remove filler words from voice-transcribed text ("uh",
  "so...", "kind of", etc.), and correct mistranscribed or imprecise
  terminology. Phrase it as you understood it; the result must be
  clear and readable.
- A very short summary of your response (ideally one sentence) as
  `**YourModelName:**` ... (e.g. `**Opus4.8:**` ...).

Use bold `**Name:**` prefixes — never `<Name>` angle-bracket markers, which
Markdown renders as (empty) HTML tags and drops. Session-level asides use a
`**Note:**` ... line the same way.
