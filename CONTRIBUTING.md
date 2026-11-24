# Contributing

Bug reports and focused improvements are welcome.

## Before opening an issue

Please include:

- The game version and BepInEx version.
- The Nobody Order Automation version.
- A short sequence that reproduces the problem.
- What you expected and what actually happened.
- Relevant BepInEx log lines, with personal paths or other sensitive data
  removed.

Back up your save before reproducing a mod issue. Do not upload save files unless
you understand what personal or account data they may contain.

## Building and testing

Install the .NET 8 SDK and provide a legitimate local game installation with
BepInEx 6 IL2CPP already initialized:

```powershell
$env:NOBODY_GAME_DIR = 'X:\Path\To\Nobody'
.\build.ps1
```

The build must complete without warnings or errors, and all dependency-free
tests must pass. Use `.\package.ps1` only when preparing a release archive.

## Contribution boundaries

Do not commit or submit:

- Game executables, assemblies, assets, saves, or other copyrighted game files.
- BepInEx binaries or generated IL2CPP interop assemblies.
- Credentials, personal paths, logs, crash dumps, or private user data.
- DRM bypasses, protection circumvention, multiplayer cheats, or unrelated
  executable injection features.

Keep changes within this mod's single-player quality-of-life scope. By
contributing code or documentation, you agree that your contribution is
provided under this project's MIT License.
