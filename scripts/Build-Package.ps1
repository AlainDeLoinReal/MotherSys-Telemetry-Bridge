param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
  function Dotnet { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$arguments) & dotnet @arguments; if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" } }
  $commit = (& git rev-parse HEAD).Trim()
  if ($LASTEXITCODE -ne 0) { throw 'Build must run from a Git checkout.' }
  if ((& git status --porcelain).Length -ne 0) { throw 'Commit the reviewed source before making a provenance package.' }
  $tag = & git describe --tags --exact-match 2>$null
  if ($LASTEXITCODE -ne 0) { $tag = $null }
  if ((& dotnet --version).Trim() -ne '8.0.425') { throw 'SDK must be 8.0.425.' }
  Dotnet @('restore','tests/Pairing/Pairing.csproj','--locked-mode')
  Dotnet @('run','--project','tests/Pairing/Pairing.csproj','--no-restore','-c','Release')
  Dotnet @('restore','tests/Lifecycle/Lifecycle.csproj','--locked-mode')
  Dotnet @('run','--project','tests/Lifecycle/Lifecycle.csproj','--no-restore','-c','Release')
  Dotnet @('restore','bridge/MotherSys.TelemetryBridge.csproj','-r','win-x64','--locked-mode')
  Dotnet @('publish','bridge/MotherSys.TelemetryBridge.csproj','-c','Release','-r','win-x64','--self-contained','true','--no-restore','-o','artifacts/Bridge')
  Copy-Item licenses artifacts/Bridge/licenses -Recurse -Force
  Copy-Item LICENSE artifacts/Bridge/licenses/MotherSys-MIT.txt -Force
  Copy-Item docs/PRIVACY.md artifacts/Bridge/PRIVACY.md -Force
  Copy-Item docs/INSTALL.md artifacts/Bridge/INSTALL.md -Force
  Dotnet @('restore','installer/Actions/Actions.csproj','--locked-mode','-p:Platform=x64')
  Dotnet @('build','installer/Actions/Actions.csproj','-c','Release','--no-restore','-p:Platform=x64')
  & python scripts/generate-installer.py
  if ($LASTEXITCODE -ne 0) { throw 'MSI file manifest generation failed.' }
  Dotnet @('build','installer/Installer.wixproj','-c','Release','-o','artifacts/Installer')
  $installer = Get-Item 'artifacts/Installer/MotherSys-Telemetry-Bridge-1.3.0-preview.msi'
  $exe = Get-Item 'artifacts/Bridge/MotherSys.TelemetryBridge.exe'
  $signature = Get-AuthenticodeSignature -LiteralPath $installer.FullName
  $metadata = [ordered]@{
    bridgeVersion = '1.3.0'; status = 'LIFECYCLE CANDIDATE - NOT APPROVED FOR PUBLIC BINARY RELEASE'
    sourceCommit = $commit; sourceTag = $tag; sdk = '8.0.425'; runtime = '8.0.31'
    origin = 'https://8a6d3033.mothersys-classic-web.pages.dev'
    installerSha256 = (Get-FileHash -LiteralPath $installer.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    exeSha256 = (Get-FileHash -LiteralPath $exe.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    authenticodeStatus = [string]$signature.Status
    signerSubject = if ($signature.SignerCertificate) { $signature.SignerCertificate.Subject } else { $null }
    signerThumbprint = if ($signature.SignerCertificate) { $signature.SignerCertificate.Thumbprint } else { $null }
    windowsInstallUninstallQA = 'NOT RUN'; windowsLiveQA = 'NOT RUN'
    buildRun = if ($env:GITHUB_RUN_ID) { "https://github.com/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID" } else { $null }
    distribution = 'LOCAL BUILD ONLY; this script does not publish or upload binaries'
  }
  $metadata | ConvertTo-Json -Depth 5 | Set-Content artifacts/RELEASE-METADATA.json -Encoding utf8
  $metadata | ConvertTo-Json -Depth 5
  if ($env:GITHUB_STEP_SUMMARY) { $metadata | ConvertTo-Json -Depth 5 | Out-File $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8 }
} finally { Pop-Location }
