# Source provenance and extraction boundary

The initial public snapshot is Bridge **1.3.0, lifecycle candidate**, source tag `v1.3.0-lifecycle-source.1`.

It started from Bridge 1.2.1 R1.2 Preview rev.1. The historical mapping in docs/history/R1.2-QA-SOURCE-MAPPING.json identifies the original private QA inputs and hashes; that binary is not distributed here and is NOT the new lifecycle candidate.

Version 1.3.0 changes installation to per-user MSI, adds complete uninstall actions and Windows Installed apps integration, scopes its state to R1.3-data, adds a current-user shutdown pipe and directs tray uninstall to Windows Settings. Collector, HTTP Host/Origin policy and cryptographic pairing remain byte-identical to the QA source baseline. No Windows sensor/renderer behavior is changed by this extraction.

This is a new standalone Git history, without any Classic Web parent commits or objects. Renderer, shaders, fonts, presets, scenes, website bundles, account information and local telemetry reports are excluded. The approved Preview address is public configuration. Source hashes are recorded by the publication audit and future package metadata.

Original code/documentation is MIT with the owner's approval. Build adds pinned SDK/runtime, locked NuGet dependencies, pinned Actions, test scripts and auditable MSI authoring. It does not imply that the historical QA binary was built by GitHub from this new tag, or that newly built bytes match the old QA executable.

All reachable public commits/blobs and author metadata are audited before push. The checked-in audit script reproduces the automated source-boundary and secret-pattern checks; manual review remains necessary. No unsigned EXE/MSI or other binary artifact is published.
