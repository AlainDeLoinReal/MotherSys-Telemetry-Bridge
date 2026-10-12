# Licensing and component provenance

The owner approved **MIT for original MotherSys code, installer, tray and documentation on 2026-10-12**. LICENSE applies to those original files; it does not relicense third-party dependencies, bundled license texts or notices.

The original Bridge source was developed for MotherSys and imported file-for-file from the R1.2 Preview rev.1 QA inputs. No third-party source has been pasted into its original C# files during this extraction. Windows APIs are called through P/Invoke and .NET interfaces. Public evidence cannot independently establish every human contribution; the owner's authorization is the basis for licensing original work. Source hashes/provenance are recorded separately.

| Component | Exact version / source | Terms / action |
|---|---|---|
| LibreHardwareMonitorLib | 0.9.4; NuGet repository commit b8077435b898d57539956388cddf49f7dacb86f7 | MPL-2.0; unmodified dependency; keep MPL notices and corresponding source access |
| ProtectedData | 8.0.0; dotnet/runtime commit 5535e31a712343a63f5d7d796cd874e563e5ac14 | MIT; keep license/notices |
| HidSharp | 2.1.0 | Apache-2.0; keep copyright/license and upstream NOTICE if supplied |
| Mono.Posix.NETStandard | 1.0.0 | NuGet's Microsoft license link points to Mono LICENSE; MIT/BSD portions and other-component inventory; retain upstream license/notice inventory, do not call every Mono file MIT |
| .NET / ASP.NET / Windows Desktop runtime | 8.0.31; SDK 8.0.425 | MIT plus third-party notices; preserve runtime redistribution notices |
| WiX SDK / DTF | 4.0.6; source commit 73c897383236ddbbbc6ba257634013c1269ceec2 | MS-RL; unmodified build tools and embedded DTF/SfxCA; preserve license and exact corresponding source pointer |
| .NET Framework reference assemblies | 1.0.3 packages | build-only reference materials under their package terms; not bundled as the application runtime |
| Other transitive NuGet packages | exact resolved lock files | package-specific MIT license and bundled third-party notices; see DEPENDENCY-INVENTORY.json and licenses/ |

[Mozilla FAQ Q8/Q11](https://www.mozilla.org/en-US/MPL/2.0/FAQ/) and [MPL 2.0 sections 3.1-3.3](https://www.mozilla.org/en-US/MPL/2.0/) permit a larger work to use separate licenses. Original Bridge files may be MIT while LibreHardwareMonitor files remain MPL. When distributing executables containing MPL code, tell recipients how to obtain that exact corresponding source and its license. If MPL files are changed later, those modifications remain MPL and must be made available as required. Do not replace dependency headers with MIT.

Source: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/b8077435b898d57539956388cddf49f7dacb86f7 . Preserve the source pointer in future installer documentation. The existing NuGet package's own repository metadata is used, rather than an inferred latest upstream release.

Some legacy packages expose URL-only licensing metadata instead of SPDX expressions. The inventory marks these and includes their retrieved notices. Mono's broader source LICENSE lists build-time components under other terms; this repository does not copy their implementation or claim that those components enter this particular Windows binary. Before a future public executable release, review the exact packaged native/managed files and their notices again. Approval of MIT does not approve a binary release, remove patent/trademark limits or establish byte-identical reproduction.

Copied notice texts are third-party materials under their original terms. See the source URL and hash inventory for exact evidence; each future package must carry all applicable notices.

WiX/DTF corresponding source: https://github.com/wixtoolset/wix/tree/73c897383236ddbbbc6ba257634013c1269ceec2 . This is an additional installer dependency; its MS-RL files remain under MS-RL and are not relicensed MIT. Original installer actions remain separate MotherSys MIT source. The source snapshot does not modify WiX library implementation.
