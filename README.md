# MotherSys Telemetry Bridge

Source for the Windows x64 local telemetry Bridge, including its installer, tray UI, authorization, pairing and collector. Original MotherSys code and documentation are MIT licensed; third-party components keep their own licenses.

Current version: **1.3.0 — lifecycle candidate**. Windows installation, native consent, DPAPI behavior, actual hardware telemetry and browser Local Network Access qualification are **NOT RUN**. There is **no approved public installer** and no executable download in this repository.

The current build accepts only `https://8a6d3033.mothersys-classic-web.pages.dev`. Other origins, including branch aliases, are rejected. Changing an origin requires a reviewed source revision and a new build.

## Inspect the implementation

- [MSI package](installer/Package.wxs) and [installer actions](installer/Actions/Actions.cs): current-user installation and complete removal.
- [Program.cs](bridge/Program.cs): tray, native consent, DPAPI storage and loopback server lifetime.
- [AccessPolicy.cs](bridge/AccessPolicy.cs): exact Host/Origin checks, CORS, preflight, method and bearer checks.
- [Pairing.cs](bridge/Pairing.cs): P-256 proof, challenges, consent, sessions and revocation.
- [WindowsCollector.cs](bridge/WindowsCollector.cs): hardware/process/network/disk reads and sample scheduling.
- [Project](bridge/MotherSys.TelemetryBridge.csproj), [SDK](global.json), [dependency lock](bridge/packages.lock.json), [runtime configuration](Directory.Build.props).

## Build and understand

[Build instructions](docs/BUILD.md) · [Architecture](docs/ARCHITECTURE.md) · [Security and limits](docs/SECURITY.md) · [Privacy](docs/PRIVACY.md) · [Install/uninstall](docs/INSTALL.md) · [Dependencies and licenses](docs/LICENSING.md) · [Release provenance](docs/RELEASES.md) · [Source provenance](docs/PROVENANCE.md) · [Windows lifecycle qualification](docs/WINDOWS-LIFECYCLE-QA.md).

This repository contains Bridge sources only. It contains no Classic Web renderer, fonts, presets, website bundle, private repository history or local telemetry reports. Public access to source is not proof that a binary is signed or safe. A hash is not an Authenticode signature or a supply-chain attestation.
