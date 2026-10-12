# Privacy policy for this implementation

Bridge reads locally and serves the latest snapshot only to an authorized browser page at the approved origin, through 127.0.0.1. The source implements no outbound telemetry upload, analytics, remote logging, update checker or cloud account connection. This is a description of reviewed code, not a guarantee about a compromised system or a modified downstream build.

| Category | Data read / exposed |
|---|---|
| Host | Windows label, machine name, uptime, random Bridge session ID, sequence and timestamps |
| CPU | total usage, logical processor count; optional temperature and clock |
| RAM | total, used, available bytes and percentage |
| Processes | count, thread count where accessible, inaccessible count; top-six CPU/memory entries with process names, PID, start timestamp, CPU percentage and working-set bytes |
| Network | aggregate download/upload bytes per second from active non-loopback/non-tunnel interfaces; interface IDs/counters are retained internally for delta calculation, not emitted individually |
| Disks | fixed drive IDs/names/mount paths, capacity, used and free bytes |
| Optional GPU/sensors | GPU identifier/name, utilization, VRAM used/total, temperature, clock and fan readings when available |

Process command lines, document contents, keystrokes, microphone/camera data, browser history and network packet contents are not read by this collector. GPU per-process metrics are not implemented. Total CPU above 64 logical processors is unavailable in this collector.

Basic collection starts when Bridge runs, regardless of whether LIVE is active. The page starts in DEMO; it requests Bridge data only after LIVE. Stopping or revoking access disconnects the page; stopping Bridge ends collection. Browser-rendered telemetry is accessible to scripts running in that authorized origin. This repository cannot guarantee the behavior of a future modified website.

Persistent local data: `%LOCALAPPDATA%\MotherSys\TelemetryBridge\R1.3-data\grants.bin` contains encrypted grants, public browser keys/fingerprints, denied/revoked keys and the sensors preference. No telemetry history is written by Bridge. HKCU stores fixed startup/protocol registration. Browser authorization storage is managed separately by the browser/website and can be cleared there. See INSTALL.md for removal.

Optional sensors may involve operating-system or vendor drivers and may fail without supported hardware or permissions. The installer does not request administrator rights or install a service. Do not disable security protections to enable a sensor.

Building from source downloads SDK/NuGet dependencies; browsing GitHub and the Preview uses those providers' normal network services. These activities are distinct from the Bridge's loopback telemetry API.
