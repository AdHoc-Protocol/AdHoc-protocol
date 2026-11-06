# Installs AdHocAgent on Windows:
#
#   irm https://raw.githubusercontent.com/AdHoc-Protocol/AdHoc-protocol/main/install.ps1 | iex
#
# Run it again to update: the configuration file AdHocAgent.toml, with your personal volatile UUID, is kept.
#
# Environment overrides:
#   ADHOC_VERSION      release tag to install, e.g. v1.2.3 (default: the latest release)
#   ADHOC_INSTALL_DIR  where the agent lives (default: %LOCALAPPDATA%\Programs\AdHocAgent)
#   ADHOC_DOWNLOAD_URL a mirror of the release files (default: the GitHub release)
#   ADHOC_NO_MODIFY_PATH=1  leave the user's PATH as it is

& {
    $ErrorActionPreference = 'Stop'
    $ProgressPreference    = 'SilentlyContinue' # Invoke-WebRequest is many times slower with its progress bar

    $repo       = 'AdHoc-Protocol/AdHoc-protocol'
    $version    = if( $env:ADHOC_VERSION ) { $env:ADHOC_VERSION } else { 'latest' }
    $installDir = if( $env:ADHOC_INSTALL_DIR ) { $env:ADHOC_INSTALL_DIR } else { Join-Path $env:LOCALAPPDATA 'Programs\AdHocAgent' }

    function Step($text) { Write-Host '==> ' -NoNewline -ForegroundColor Cyan; Write-Host $text }
    function Warn($text) { Write-Host 'warning: ' -NoNewline -ForegroundColor Yellow; Write-Host $text }
    function Fail($text) { Write-Host 'error: ' -NoNewline -ForegroundColor Red; Write-Host $text; throw 'AdHocAgent was not installed.' }

    $arch = switch( [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture ) {
        'X64'   { 'x64' }
        'Arm64' { 'arm64' }
        default { Fail "the $_ processor is not supported: AdHocAgent is built for x64 and arm64." }
    }
    $asset = "AdHocAgent-win-$arch.zip"
    $base  = if( $env:ADHOC_DOWNLOAD_URL ) { $env:ADHOC_DOWNLOAD_URL.TrimEnd('/') } elseif( $version -eq 'latest' ) { "https://github.com/$repo/releases/latest/download" } else { "https://github.com/$repo/releases/download/$version" }

    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("adhoc-agent-" + [guid]::NewGuid())
    New-Item -ItemType Directory -Path $tmp | Out-Null
    try {
        Step "Downloading AdHocAgent ($version, win-$arch)"
        $zip = Join-Path $tmp $asset
        try { Invoke-WebRequest -UseBasicParsing -Uri "$base/$asset" -OutFile $zip }
        catch { Fail "cannot download $base/$asset. Check the network, or the releases page: https://github.com/$repo/releases" }

        $sums = $null
        try { $sums = (Invoke-WebRequest -UseBasicParsing -Uri "$base/SHA256SUMS.txt").Content } catch { }
        if( $sums ) {
            if( $sums -is [byte[]] ) { $sums = [System.Text.Encoding]::UTF8.GetString($sums) }
            $line = $sums -split "`n" | Where-Object { ($_ -split '\s+')[1] -in @($asset, "*$asset") } | Select-Object -First 1
            if( -not $line ) { Fail "SHA256SUMS.txt of the release has no line for $asset." }
            $expected = ($line -split '\s+')[0]
            $actual   = (Get-FileHash -Algorithm SHA256 -Path $zip).Hash
            if( $expected -ne $actual ) { Fail "the checksum of $asset does not match the release: the download is damaged. Run the script again." }
            Step 'Checksum verified'
        }
        else { Warn 'the release has no SHA256SUMS.txt: the download is not verified.' }

        Step "Installing into $installDir"
        $unpacked = Join-Path $tmp 'unpacked'
        Expand-Archive -Path $zip -DestinationPath $unpacked -Force
        # AdHocAgent.toml beside the executable holds the personal volatile UUID: an update never replaces it
        if( Test-Path (Join-Path $installDir 'AdHocAgent.toml') ) { Remove-Item (Join-Path $unpacked 'AdHocAgent.toml') -ErrorAction SilentlyContinue }
        New-Item -ItemType Directory -Force -Path $installDir | Out-Null
        try { Copy-Item -Path (Join-Path $unpacked '*') -Destination $installDir -Recurse -Force }
        catch { Fail "cannot write into $installDir - is AdHocAgent running? Close it and run the script again." }
    }
    finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }

    # the command `AdHocAgent` in every new terminal: the folder goes on the user's PATH once
    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $onPath   = ($userPath -split ';' | Where-Object { $_.TrimEnd('\') -ieq $installDir.TrimEnd('\') })
    if( -not $onPath -and $env:ADHOC_NO_MODIFY_PATH -ne '1' ) {
        [Environment]::SetEnvironmentVariable('Path', ((@($userPath -split ';' | Where-Object { $_ }) + $installDir) -join ';'), 'User')
        $env:Path = "$env:Path;$installDir"
        Step 'Added to your PATH'
    }

    # AdHocAgent is framework-dependent: it runs on an installed .NET 10
    $runtimes = try { & dotnet --list-runtimes 2>$null } catch { $null }
    if( -not ($runtimes -match '^Microsoft\.NETCore\.App 10\.') ) {
        Warn 'AdHocAgent runs on .NET 10, which is not found. Install the .NET 10 SDK (a description project needs it too):'
        Write-Host '         https://dotnet.microsoft.com/download/dotnet/10.0'
    }

    Write-Host ''
    Write-Host 'AdHocAgent is installed.' -ForegroundColor Green
    Write-Host 'Run AdHocAgent to start (in a new terminal, if this one does not find it).'
    Write-Host "Next: get your personal volatile UUID - https://github.com/$repo/blob/main/MANUAL.md#step-2-get-your-personal-volatile-uuid" -ForegroundColor DarkGray
}
