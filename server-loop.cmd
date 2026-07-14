:<<BATCH
    @echo off
    pwsh -NoProfile -File "%~dp0server-loop.ps1" %*
    exit /b %ERRORLEVEL%
BATCH

#!/bin/sh
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
pwsh -NoProfile -File "$SCRIPT_DIR/server-loop.ps1" "$@"
exit $?
