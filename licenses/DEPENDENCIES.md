# Third-party source and notices

Original MotherSys files are MIT (root LICENSE). This directory contains upstream materials under their own terms. See docs/DEPENDENCY-INVENTORY.json and docs/LICENSING.md.

LibreHardwareMonitor 0.9.4 exact package source: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/b8077435b898d57539956388cddf49f7dacb86f7 (MPL-2.0).
.NET/ASP.NET/Windows Desktop 8.0.31: https://github.com/dotnet/runtime/tree/v8.0.31 , https://github.com/dotnet/aspnetcore/tree/v8.0.31 , https://github.com/dotnet/wpf/tree/v8.0.31 , https://github.com/dotnet/winforms/tree/v8.0.31 . Preserve THIRD-PARTY-NOTICES as well as MIT.
HidSharp 2.1.0 package license URL: https://www.zer7.com/files/oss/hidsharp/LICENSE.txt ; retrieved text is Apache-2.0. Its header is maintained by upstream and currently says 2010-2025; the package metadata copyright remains 2010-2019.
Mono.Posix.NETStandard 1.0.0 license URL redirects to https://github.com/mono/mono/blob/main/LICENSE . Included LICENSE and PATENTS text are upstream documents, not a conclusion that every Mono component is present in this build.

Some retained runtime notices cover components outside this Windows package. The generated dependency inventory distinguishes exact NuGet packages. No dependency implementation binaries or third-party source trees are vendored here.

WiX SDK/DTF 4.0.6 (including the unmanaged custom-action wrapper): https://github.com/wixtoolset/wix/tree/73c897383236ddbbbc6ba257634013c1269ceec2 ; MS-RL retained in WiX-4.0.6-MS-RL.txt. Original installer action code is separately MIT.
