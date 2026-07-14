# server-loop.ps1
# Edit-run-restart loop for the BoardGames server.
#
# Runs two steps in sequence:
#   1) dotnet-build  - dotnet build -c $Configuration src/Host/Host.csproj
#   2) server-run    - runs the built BoardGames server on http://localhost:$Port
#
# Per-step output goes to tmp/server-loop-<step>.log. Stage transitions are
# appended to tmp/server-loop.log. On any failure, a final marker line
# "Last step failed, remove this file to restart the loop." is appended to
# tmp/server-loop.log, and the script waits for either a keypress (in the
# loop terminal) or removal of tmp/server-loop.log to restart. A server stop
# (keypress, restart marker, or watchdog kill) restarts the loop.
#
# To trigger a rebuild+restart from another terminal (or from an AI agent):
#   create tmp/server-loop.restart — the loop stops the server, deletes the
#   marker, and starts a fresh iteration.
#
# All loop log files are wiped at the start of every iteration.

[CmdletBinding()]
param(
    [Alias('c')]
    [string]$Configuration = "Debug",
    [int]$Port = 5030,
    # Anything after the named params is forwarded to the BoardGames server
    # as command-line arguments.
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ServerArgs = @()
)

$ErrorActionPreference = "Continue"
$ScriptDir = $PSScriptRoot
Set-Location $ScriptDir

$tmpDir = Join-Path $ScriptDir "tmp"
if (-not (Test-Path $tmpDir)) {
    New-Item -ItemType Directory -Path $tmpDir | Out-Null
}

$pidFile         = Join-Path $tmpDir "server-loop.pid"
$loopLog         = Join-Path $tmpDir "server-loop.log"
$dotnetBuildLog  = Join-Path $tmpDir "server-loop-dotnet-build.log"
$serverRunBase   = Join-Path $tmpDir "server-loop-server-run"
$serverRunOutLog = "$serverRunBase.out"  # server stdout
$serverRunErrLog = "$serverRunBase.err"  # server stderr (empty on a healthy run)
$restartMarker   = Join-Path $tmpDir "server-loop.restart"

$allLoopLogs = @($loopLog, $dotnetBuildLog, $serverRunOutLog, $serverRunErrLog)
$baseUri = "http://localhost:$Port"

function Write-LoopLog([string]$Text) {
    $ts = Get-Date -Format "HH:mm:ss"
    $line = "[$ts] $Text"
    Write-Host $line
    Add-Content -Path $loopLog -Value $line -Encoding UTF8
}

function Reset-LoopLogs {
    foreach ($f in $allLoopLogs) {
        Remove-Item $f -Force -ErrorAction SilentlyContinue
    }
}

function Wait-ForRestart {
    Write-LoopLog "Last step failed, remove this file to restart the loop."
    Write-Host "Press any key in this terminal OR delete '$loopLog' to restart..." -ForegroundColor Yellow
    while (Test-Path $loopLog) {
        try {
            if ([System.Console]::KeyAvailable) {
                [void][System.Console]::ReadKey($true)
                break
            }
        } catch {
            # No interactive console (e.g. running detached); only file removal will break the wait.
        }
        Start-Sleep -Milliseconds 333
    }
}

function Test-ServerProbe {
    try {
        $resp = Invoke-WebRequest -Uri $baseUri -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
        return ([int]$resp.StatusCode -ge 200) -and ([int]$resp.StatusCode -lt 400)
    } catch {
        return $false
    }
}

function Start-ServerProcess {
    param(
        [Parameter(Mandatory)] [string]   $ServerBinary,
        [Parameter(Mandatory)] [AllowEmptyCollection()] [string[]] $ServerArgs,
        [Parameter(Mandatory)] [string]   $StdoutPath,
        [Parameter(Mandatory)] [string]   $StderrPath
    )
    if ($IsWindows) {
        $startArgs = @{
            FilePath               = $ServerBinary
            RedirectStandardOutput = $StdoutPath
            RedirectStandardError  = $StderrPath
            NoNewWindow            = $true
            PassThru               = $true
        }
        if ($ServerArgs.Count -gt 0) { $startArgs.ArgumentList = $ServerArgs }
        return Start-Process @startArgs
    }
    # POSIX: Start-Process -RedirectStandardOutput/-RedirectStandardError can
    # abort the PowerShell host on force-kill (disposed stream writers), so
    # launch via /bin/sh with shell-level redirection instead.
    $quotedArgs = ($ServerArgs | ForEach-Object { "'$(($_ -replace "'", "'\''"))'" }) -join ' '
    $shellLine = "exec '$ServerBinary' $quotedArgs > '$StdoutPath' 2> '$StderrPath'"
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = '/bin/sh'
    $psi.ArgumentList.Add('-c')
    $psi.ArgumentList.Add($shellLine)
    $psi.UseShellExecute = $false
    return [System.Diagnostics.Process]::Start($psi)
}

function Stop-ServerProcess([System.Diagnostics.Process]$Proc, [string]$Reason) {
    Write-LoopLog "Stopping the server (PID $($Proc.Id)): $Reason"
    try { Stop-Process -Id $Proc.Id -Force -ErrorAction Stop }
    catch { Write-LoopLog "Stop-Process failed: $_" }
}

# The PID file is how outside observers (e.g. AI agents) detect a running
# loop: if the file is missing or its PID is dead, the loop is not running.
Set-Content -Path $pidFile -Value $PID

# One-time banner: listing log paths here keeps per-iteration output terse.
Write-Host "server-loop log files (wiped at the start of every iteration):"
Write-Host "  loop          $loopLog"
Write-Host "  dotnet-build  $dotnetBuildLog"
Write-Host "  server-run    $serverRunOutLog (stdout)"
Write-Host "                $serverRunErrLog (stderr)"
Write-Host "Server URL: $baseUri"
Write-Host "Restart from another terminal: create $restartMarker"
Write-Host ""

while ($true) {
    Reset-LoopLogs
    Remove-Item $restartMarker -Force -ErrorAction SilentlyContinue
    $failureMessage = $null

    # Step 1: dotnet-build
    Write-LoopLog "Step 1/2 (dotnet-build)"
    & dotnet build -c $Configuration src/Host/Host.csproj *> $dotnetBuildLog
    if ($LASTEXITCODE -ne 0) {
        $failureMessage = "Step 1 (dotnet-build) failed with exit code $LASTEXITCODE."
    }

    # Step 2: server-run
    if (-not $failureMessage) {
        Write-LoopLog "Step 2/2 (server-run)"
        $binDir = Join-Path $ScriptDir "src/Host/bin/$Configuration/net10.0"
        $serverBinary = Join-Path $binDir ($IsWindows ? "BoardGames.Host.exe" : "BoardGames.Host")
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $env:ASPNETCORE_URLS = $baseUri
        $proc = Start-ServerProcess `
            -ServerBinary $serverBinary `
            -ServerArgs   $ServerArgs `
            -StdoutPath   $serverRunOutLog `
            -StderrPath   $serverRunErrLog
        Write-Host "Keyboard: any key = stop the server and restart the loop." -ForegroundColor Cyan

        # Watchdog state: probe $baseUri every 15s (first probe 10s after
        # start); two consecutive misses -> force-kill so the loop recycles
        # into a fresh build.
        $watchdogMissCount = 0
        $watchdogNextProbeAt = (Get-Date).AddSeconds(10)
        # Set whenever the loop knows it caused the exit; on exit non-zero
        # codes are then treated as a stop, not a start failure.
        $stopRequested = $false
        $everProbeOk = $false

        while (-not $proc.HasExited) {
            try {
                if ([System.Console]::KeyAvailable) {
                    [void][System.Console]::ReadKey($true)
                    Stop-ServerProcess $proc "keypress in the loop terminal"
                    $stopRequested = $true
                }
            } catch {
                # Detached / no console — fall back to passive wait.
            }

            if ((Test-Path $restartMarker) -and -not $stopRequested) {
                Stop-ServerProcess $proc "restart marker found ($restartMarker)"
                $stopRequested = $true
            }

            if ((Get-Date) -ge $watchdogNextProbeAt -and -not $stopRequested) {
                if (Test-ServerProbe) {
                    $everProbeOk = $true
                    $watchdogMissCount = 0
                } else {
                    $watchdogMissCount++
                    Write-LoopLog "Watchdog: miss $watchdogMissCount/2 on $baseUri (PID $($proc.Id))."
                    if ($watchdogMissCount -ge 2) {
                        Stop-ServerProcess $proc "watchdog: two consecutive misses"
                        $stopRequested = $true
                    }
                }
                $watchdogNextProbeAt = (Get-Date).AddSeconds(15)
            }

            Start-Sleep -Milliseconds 200
        }
        $exitCode = $proc.ExitCode
        if ($stopRequested -or $everProbeOk) {
            Write-LoopLog "Server stopped (exit code $exitCode). Restarting from step 1..."
        } elseif ($exitCode -ne 0) {
            $failureMessage = "Step 2 (server-run) failed to start, exit code $exitCode."
        } else {
            Write-LoopLog "Server stopped. Restarting from step 1..."
        }
    }

    if ($failureMessage) {
        Write-LoopLog $failureMessage
        Wait-ForRestart
    }
}
