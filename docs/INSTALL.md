# Per-user installation and complete removal (1.3.0 lifecycle candidate)

No official public installer is approved. New MSI is a source/build candidate; actual Windows 11 installation/uninstallation remains NOT RUN. The old unsigned 1.2.1 QA EXE has only the historical partial removal behavior and is not this MSI.

## Install

Build the MSI according to BUILD.md, or receive a separately approved package. Open the local MSI as a normal user. Confirm the program name **MotherSys Telemetry Bridge**, version **1.3.0**, publisher **MotherSys** (a metadata label, not a certificate). Optional sign-in startup is unchecked. The payload is embedded; installing/uninstalling does not download code or require internet. The MSI custom action runtime uses Windows 11's .NET Framework 4.8/4.8.1; Bridge itself contains .NET 8.

Program files: `%LOCALAPPDATA%\MotherSys\TelemetryBridge\R1.3`.
Saved state: `%LOCALAPPDATA%\MotherSys\TelemetryBridge\R1.3-data\grants.bin`.
No migration or deletion of R1.2 QA folders/grants is performed. Another installation that owns the shared launch protocol must be removed using its own uninstall controls before installing this candidate; the new MSI refuses to overwrite an unrelated owner. An existing MSI in this product family must be uninstalled normally before a new candidate is installed.

Successful interactive installation starts Bridge in tray and creates a Start menu shortcut. Open https://8a6d3033.mothersys-classic-web.pages.dev/ exactly, click LIVE and authorize the exact origin through normal browser/Bridge prompts. Everyday use still starts the page in DEMO and requires one LIVE click; Windows testing of this behavior is NOT RUN.

## Registry changes

- Windows Installer creates its product's per-user Installed apps/uninstall record, DisplayName, DisplayVersion, Manufacturer/Publisher and UninstallString. It removes this record during uninstall.
- HKCU `Software\Microsoft\Windows\CurrentVersion\Run`, value `MotherSysTelemetryBridgeR13`, only if optional startup is selected (or later enabled in tray).
- HKCU `Software\Classes\mothersys-telemetry`, its default label/URL Protocol value and fixed `shell\open\command` pointing only to the installed EXE with --open.
- HKCU `Software\MotherSys\TelemetryBridge\R1.3` file/shortcut component markers authored by MSI.

Uninstall checks Run/protocol ownership before removing values. It does not remove an entry that now belongs to another version/application. No HKLM service, global MotherSys deletion or administrator requirement is authored.

## Uninstall

Windows Settings -> Apps -> Installed apps -> **MotherSys Telemetry Bridge** -> **Uninstall**. The confirmation shows version and the checkbox **Remove saved settings and website permissions**, checked by default. Cancel leaves the application installed. Tray also offers a link to Windows Installed apps.

After confirmation, C# actions stop the exact installed Bridge through its current-user pipe, wait for process exit and use verified-path termination only if needed. The listener and in-memory sessions end with the process. All saved grants are revoked. The MSI removes its own EXE, DLLs, licenses, shortcut, component registry entries and Installed apps entry; the Windows Installer engine handles uninstall code cleanup. No manual file/registry deletion or network script is part of normal removal.

Checked: delete grants.bin and grants.bin.tmp in R1.3-data; remove that directory only if empty. Unchecked: keep only the DPAPI-protected optional-sensor preference, clearing grants and browser public-key records. No website remains authorized; reinstall requires consent. Browser-side keys/data are separate and remain in the browser unless cleared there.

General MotherSys folders, other versions/apps and unknown files are preserved. If a process cannot be identified/stopped, data is corrupt while preservation is requested, a path is redirected, or MSI file removal fails, the operation reports failure rather than claiming complete removal. Missing state/program files are tolerated by the appropriate owned-file removal path. A damaged package can be repaired/reinstalled from the same local MSI and then removed; users do not have to edit registry or manually delete directories. Windows lifecycle tests, including these recovery paths, are pending.

Windows Installer may retain its system-managed package/rollback cache; the application does not delete operating-system caches. A locally downloaded installer/source ZIP outside the installation path is user-owned and is not deleted by uninstall. No security protections are disabled.
