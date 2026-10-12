# Security controls and known limits

Listener: IPv4 `http://127.0.0.1:47631`. Incoming remote IP must be loopback; Host must be exactly `127.0.0.1:47631`. Origin must be the canonical exact approved HTTPS origin. No wildcard/subdomain suffix is permitted. Origin and Host checks happen before endpoint dispatch. CORS reflects only that permitted origin, with Vary: Origin and no-store. Methods and requested headers are allowlisted; preflight supports the browser private-network mechanism. Browser permission is separate from Bridge consent.

Pairing uses a 30-second one-use random challenge, bound to origin, key fingerprint and Bridge session. ECDSA P-256/SHA-256 with P1363 signature encoding proves possession of the browser key. A new browser requires a native consent callback; stored grants require fresh proof on reconnect. Bearer sessions last 60 seconds and are bound to exact origin. They do not persist to disk. Revocation invalidates sessions/challenges and in-flight consent by generation; revoked keys stay blocked until the user resets them in tray.

Bounds: 4096-byte request body, 32 concurrent Kestrel connections, 10-second request-header timeout, 64 challenges/sessions/grants, bounded blocked-key store. Unsupported endpoints/methods are refused. Optional hardware sensors are disabled by default; their library may attempt low-level hardware access when explicitly enabled. No requirement to disable Windows defenses or elevate the main program is imposed. Unsupported sensors stay unavailable.

## Limits

Loopback HTTP is not encrypted transport. Browser origin controls are not protection against a hostile local process. A compromised Windows account, malicious browser extension or script in the authorized website origin can defeat these assumptions. DPAPI CurrentUser does not protect against malware running as that user. The browser's non-extractable key is a website-client behavior; it is not enforced by the C# server and is not a defense against a compromised browser.

No independent penetration audit, signed installer qualification or actual Windows LIVE qualification is claimed. Source/core tests and synthetic local Kestrel transport checks passed during preparation. They do not prove behavior of the packaged EXE on Windows, native prompts, DPAPI or real sensors. Background lifecycle qualification is also pending.

Do not post grants.bin, keys, bearer tokens, private process reports or hardware-identifying screenshots to public issues. Report a suspected secret exposure privately through the repository owner's normal private contact route; no invented security contact is provided.

## Installer/uninstaller boundary

MSI is per-user and carries its payload in an embedded cabinet. C# actions execute impersonating the current user. Program and data locations are fixed and reparse points are refused. Only the exact revision process path can be stopped. Run/protocol cleanup checks values against this revision; no whole MotherSys directory or broad registry subtree is deleted. Windows Installer owns its file manifest and Installed apps record. Other MSI/QA versions are not automatically removed. Keeping settings clears every saved grant and retains only the sensor preference. Actual Windows lifecycle qualification is NOT RUN.
