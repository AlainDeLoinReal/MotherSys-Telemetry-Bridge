# Test scope

Pairing tests link the production Pairing.cs and AccessPolicy.cs. They use synthetic in-memory grants and a controllable clock. They check consent, reconnect, exact origin, replay, malformed proof, expiry, capacity and revocation. They read no Windows telemetry and store no real authorization data.

Native tray consent, installer, DPAPI, optional sensor behavior, Local Network Access dialogs, actual hardware and the packaged Windows executable remain NOT RUN. Repository audit and successful compilation do not qualify these behaviors.

## Lifecycle policy checks

`dotnet run --project tests/Lifecycle` checks owned path identity, removal boundaries, settings retention without website grants, malformed data handling and rejection of reparse paths. These portable checks do not qualify Windows Installer, registry changes, tray shutdown or real Windows 11 uninstall. See `docs/WINDOWS-LIFECYCLE-QA.md`; those tests remain NOT RUN.
