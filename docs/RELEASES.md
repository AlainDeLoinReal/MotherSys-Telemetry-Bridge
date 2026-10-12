# Release provenance and publication gates

Current source tag: v1.3.0-lifecycle-source.3. Current Bridge version: 1.3.0 lifecycle candidate. No official public installer exists. The private QA installer is unsigned. No GitHub Release or public QA executable asset is authorized.

Every future official installer must state:

- Bridge version, public source tag and full commit SHA;
- build workflow/run URL, OS, SDK/runtime and locked dependencies;
- SHA-256 of EXE and MSI (and ZIP if one is distributed);
- Authenticode status, signer and certificate thumbprint where applicable;
- instructions to build from the same tag and exact corresponding dependency sources;
- Windows qualification results and known limitations.

scripts/Build-Package.ps1 prepares this metadata for locally built packages. The public workflow executes it with a read-only token, records hashes and signing status in the run summary, and uploads no unsigned binary. It never publishes a Release. Build logs are auditable but are not provenance attestations and do not establish that a separately downloaded file came from that run.

For a future public release, obtain separate approval, qualify Windows behavior, adopt a reviewed signing process, verify Authenticode with Get-AuthenticodeSignature and an appropriate Windows trust check, then compute hashes AFTER signing. Include applicable third-party notices and source pointers. Record the final signed bytes and exact source commit; do not reuse hashes of pre-signing files. A hash manifest never substitutes for signing or an independently verifiable provenance record.

A downstream self-build may remain unsigned and must be labeled as such. It is not the project's official installer. No workflow in this source snapshot holds signing keys, credentials or a release-publishing token.
