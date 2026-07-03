param(
    [switch]$WaitOnly
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Test-RedisPort {
    param([string]$HostName = "127.0.0.1", [int]$Port = 6379)
    try {
        return (Test-NetConnection -ComputerName $HostName -Port $Port -WarningAction SilentlyContinue).TcpTestSucceeded
    }
    catch {
        return $false
    }
}

function Start-PortableRedis {
    $redisDir = Join-Path $repoRoot "tools\redis"
    $server = Join-Path $redisDir "redis-server.exe"
    $config = Join-Path $redisDir "redis.windows.conf"

    if (-not (Test-Path $server)) {
        Write-Host "Portable Redis not found. Downloading to tools\redis ..."
        New-Item -ItemType Directory -Force -Path $redisDir | Out-Null
        $zip = Join-Path $env:TEMP "Redis-x64-3.0.504.zip"
        Invoke-WebRequest -Uri "https://github.com/microsoftarchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.zip" -OutFile $zip
        Expand-Archive -Path $zip -DestinationPath $redisDir -Force
    }

    if (Test-RedisPort) {
        Write-Host "Redis already listening on 127.0.0.1:6379"
        return
    }

    Write-Host "Starting portable Redis from tools\redis ..."
    Start-Process -FilePath $server -ArgumentList "`"$config`"" -WindowStyle Hidden
}

if (Get-Command docker -ErrorAction SilentlyContinue) {
    Write-Host "Starting Redis via docker-compose.dev.yml ..."
    docker compose -f docker-compose.dev.yml up -d redis
}
else {
    Write-Host "Docker not found — using portable Redis in tools\redis."
    Start-PortableRedis
}

if ($WaitOnly) {
    exit 0
}

$deadline = (Get-Date).AddMinutes(2)
while ((Get-Date) -lt $deadline) {
    if (Test-RedisPort) {
        $cli = Join-Path $repoRoot "tools\redis\redis-cli.exe"
        if (Test-Path $cli) {
            $pong = & $cli ping 2>$null
            if ($pong -eq "PONG") {
                Write-Host "Redis is ready on 127.0.0.1:6379 (PONG)"
                exit 0
            }
        }
        else {
            Write-Host "Redis port 6379 is open"
            exit 0
        }
    }

    Start-Sleep -Seconds 2
}

Write-Error "Redis did not become ready within 2 minutes."
exit 1
