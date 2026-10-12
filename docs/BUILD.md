# Build from source

## Requirements

Windows 11 x64 (including .NET Framework 4.8 or 4.8.1 for MSI custom actions), Python 3 for the file-manifest generator, and the official .NET SDK **8.0.425**, selected by global.json. No administrator privileges are needed to build or install Bridge. Restore needs nuget.org; normal Bridge operation does not. Linux can run the pairing tests and cross-compile the Windows target; it cannot qualify installation, tray, DPAPI or hardware collection.

Clone https://github.com/AlainDeLoinReal/MotherSys-Telemetry-Bridge and check out the exact published source tag (reviewed candidate tag: `v1.3.0-lifecycle-source.3`). Record `git rev-parse HEAD` before building.

```powershell
git clone https://github.com/AlainDeLoinReal/MotherSys-Telemetry-Bridge.git
cd MotherSys-Telemetry-Bridge
git checkout v1.3.0-lifecycle-source.3
git rev-parse HEAD
dotnet --version
dotnet restore tests/Pairing/Pairing.csproj --locked-mode
dotnet run --project tests/Pairing/Pairing.csproj --no-restore
dotnet restore bridge/MotherSys.TelemetryBridge.csproj -r win-x64 --locked-mode
dotnet publish bridge/MotherSys.TelemetryBridge.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts/Bridge
```

Do not install just the EXE. The package also needs adjacent native DLLs and dependency license notices. To make a locally built complete package with source/version/hash/signature metadata:

```powershell
pwsh -File scripts/Build-Package.ps1
```

The script restores locked packages, runs tests, builds, copies licenses and produces a local MSI and release metadata JSON with EXE/MSI hashes and Authenticode status. The script also builds the MSI and its custom uninstall actions using WiX 4.0.6. It performs no upload, signing or release publication. Your local build is not an approved official installer.

SDK 8.0.425, runtime 8.0.31, lock files and deterministic compiler settings reduce dependency drift. They do not establish cross-machine byte reproducibility. Native bundling, build paths and SDK/runtime/platform differences may change bytes. No byte-identical reproduction claim is made. Do not silently update dependencies in a qualification run; review new locks and requalify relevant behavior.

## GitHub build

The public Actions workflow builds and tests on Windows, on push/PR and manual dispatch. Actions are pinned to reviewed commit SHAs and permissions are read-only. The workflow records source commit and installer/EXE hashes in the run summary, then runs the repository audit. It uploads no unsigned binaries and creates no Releases. Code signing and release approval are separate gates.

MSI validation remains enabled. ICE91 warnings identify the intentionally fixed per-user paths; this package does not offer per-machine installation. Empty parent directories are scheduled with standard RemoveFolder rows, which preserve non-empty folders and other MotherSys components. See [Microsoft ICE64](https://learn.microsoft.com/en-us/windows/win32/msi/ice64), [ICE91](https://learn.microsoft.com/en-us/windows/win32/msi/ice91) and [WiX RemoveFolder](https://docs.firegiant.com/wix/schema/wxs/removefolder/). Actual Windows uninstall preservation still needs the documented QA.
