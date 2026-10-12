# Architecture

```text
Browser after explicit LIVE click
  -> HTTP 127.0.0.1:47631 (browser Local Network Access policy also applies)
  -> exact Host and HTTPS Origin allowlist
  -> challenge / proof / native consent
  -> short-lived origin-bound bearer
  -> telemetry JSON read from the latest in-memory collector sample
```

The MSI package and C# custom actions handle installation and removal. Program.cs contains the Windows Forms tray application and Kestrel host. There is no Windows service. HKCU startup is optional and unchecked by default. A fixed custom URI handler starts only the installed executable; it accepts no incoming URL or command arguments. A named mutex limits duplicate instances. Every origin authorization uses a native Windows prompt with the exact website address.

The lifecycle candidate uses a separate R1.3 program directory and R1.3-data state directory. A current-user-only named pipe requests graceful shutdown; the uninstaller waits for the exact program process to exit, with a verified-path termination fallback. Windows Installer removes its own files and Installed apps record; there is no self-deleting script or downloaded uninstall code.

GrantStore serializes grants, denied/revoked browser keys and the optional-sensors setting. Windows DPAPI CurrentUser protects grants.bin. The browser holds its own private signing key; Bridge stores only the browser public key/fingerprint and origin. Sessions and challenges live in memory.

The collector publishes protocol mothersys.telemetry version 1. Core CPU/RAM/network are sampled every second; process and optional sensor collection every two seconds; disks every 30 seconds. Every section has sampledAt and maxAgeMs. Failed/unsupported measurements are null, not invented zeroes. Bridge continues collecting while running even if no browser is connected. Collection does not write a telemetry history to disk.

Endpoints: GET /v1/challenge, POST /v1/session (proof, not administrative control), GET /v1/telemetry (requires bearer). No remote control, upload, process-kill or administrative HTTP endpoint is implemented. See SECURITY.md for limits and PRIVACY.md for exact fields.

Website rendering, weather and audio simulation are outside this source repository.
