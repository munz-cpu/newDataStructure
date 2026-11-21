#requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position=0)][string]$Player,
    [Parameter(Position=1)][string]$Server,
    [switch]$OfflineOnly
)

$ErrorActionPreference = 'Stop'
$exe = (Resolve-Path -LiteralPath $Player).Path
$work = Join-Path ([IO.Path]::GetTempPath()) ("divinewar-" + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($work) | Out-Null
$processes = [Collections.Generic.List[Diagnostics.Process]]::new()

function Start-Player([string]$Role) {
    $start = [Diagnostics.ProcessStartInfo]::new($exe)
    $start.UseShellExecute = $false
    $start.Environment['DIVINEWAR_SMOKE_DIR'] = $work
    $start.Environment['DIVINEWAR_SMOKE_ROLE'] = $Role
    $start.Environment['DIVINEWAR_UGS_PROFILE'] = $Role
    foreach ($argument in @('-batchmode', '-nographics', '-screen-width', '640', '-screen-height', '480', '-logFile', (Join-Path $work "$Role.log"))) {
        $start.ArgumentList.Add($argument)
    }
    $process = [Diagnostics.Process]::Start($start)
    $processes.Add($process)
    return $process
}

function Assert-Player([Diagnostics.Process]$Process, [string]$Role, [int]$Seconds) {
    if (!$Process.WaitForExit($Seconds * 1000)) { throw "$Role smoke timed out." }
    if ($Process.ExitCode -ne 0 -or !(Test-Path -LiteralPath (Join-Path $work "$Role.passed"))) {
        throw "$Role smoke failed with exit code $($Process.ExitCode)."
    }
}

try {
    $offline = Start-Player 'offline'
    Assert-Player $offline 'offline' 75
    Write-Host 'Solo simulation ran without initializing Unity Services and returned to Title.'
    if (!$OfflineOnly) {
        $serverInfo = [Diagnostics.ProcessStartInfo]::new((Resolve-Path -LiteralPath $Server).Path)
        $serverInfo.UseShellExecute = $false
        $serverInfo.Environment['DIVINEWAR_UGS_PROFILE'] = 'server'
        foreach ($argValue in @('-batchmode', '-nographics', '-logFile', (Join-Path $work 'server.log'))) {
            $serverInfo.ArgumentList.Add($argValue)
        }
        $processes.Add([Diagnostics.Process]::Start($serverInfo))
        $first = Start-Player 'first'
        $second = Start-Player 'second'
        Assert-Player $first 'first' 240
        Assert-Player $second 'second' 240
        Write-Host 'Two clients exchanged commands and state with the dedicated server across two games.'
    }
} catch {
    Get-ChildItem -LiteralPath $work -Filter '*.log' | ForEach-Object {
        Select-String -LiteralPath $_.FullName -SimpleMatch 'Relay smoke failed (' |
            Select-Object -Last 1 | ForEach-Object { Write-Warning $_.Line }
    }
    throw
} finally {
    foreach ($process in $processes) {
        if (!$process.HasExited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        $process.Dispose()
    }
    Remove-Item -LiteralPath $work -Force -Recurse
}
