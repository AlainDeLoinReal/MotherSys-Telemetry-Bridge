# Required Windows 11 lifecycle qualification

Every row is **NOT RUN** until tested on real Windows 11. Compilation, synthetic tests, Wine packaging and GitHub runner package construction do not establish actual installation/uninstallation behavior.

Use one exact MSI, source commit and SHA-256. Record Windows/browser versions and Authenticode status. Do not publish raw grants or private telemetry. No security bypass is part of this protocol.

| Trial | Acceptance | Status |
|---|---|---|
| Standard-user install | few clicks, no administrator/UAC/service | NOT RUN |
| Installed apps | exact name, version, publisher, functioning Uninstall | NOT RUN |
| Cancel uninstall | process/files/registrations/data unchanged | NOT RUN |
| Uninstall while tray is running | exact process exits and loopback listener ends | NOT RUN |
| Startup selected then removal | own HKCU Run value removed, unrelated values retained | NOT RUN |
| Protocol cleanup | own handler removed; another owner's handler preserved | NOT RUN |
| Default data removal | grants.bin/.tmp removed; other MotherSys data retained | NOT RUN |
| Keep settings | sensor preference remains; grants/public-key records removed | NOT RUN |
| Reinstall | clean startup, consent required again, settings policy respected | NOT RUN |
| Missing program/state files | removal succeeds or reports specific recoverable MSI failure | NOT RUN |
| Corrupt grants | removal option works; preservation fails with a clear retry choice | NOT RUN |
| Repeated uninstall | no unrelated deletions; already-removed MSI handled by Windows | NOT RUN |
| Other MotherSys sentinel | files/registry belonging to other components unchanged | NOT RUN |
| Final processes/listener | no process from R1.3 installation and no listener it owned | NOT RUN |
| No internet | removal succeeds with network disconnected | NOT RUN |
| DEMO -> LIVE -> reopen | native consent first, normal one-click reconnect later | NOT RUN |

Before/after record the exact installation/data directories, owned registry values, Installed apps entry, process executable paths and listener owner PID. Place synthetic sentinel files/registry values for an unrelated test component and confirm they are untouched. Exercise optional-sensor settings without enabling unsupported drivers. If MSI/SmartScreen policy blocks the unsigned candidate, record BLOCKED and retain Windows protections.
