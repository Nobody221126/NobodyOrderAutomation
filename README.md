# Nobody Order Automation

Nobody Order Automation is an open-source quality-of-life mod for
**Nobody - The Turnaround**. It adds a native-looking **Ship All** button to the
Warehouse so a large order queue can be handled with one click.

The mod runs locally through BepInEx. It does not connect to a server, collect
analytics, or require a separate helper program.

## What it does

- Ships every order that currently has enough inventory.
- Skips orders without enough stock and leaves them pending for later.
- Handles long, non-contiguous order lists, including off-screen entries.
- Refreshes the visible order list and inventory after processing.
- Skips the packing animation without advancing in-game time.
- Uses the game's normal delivered-order state so revenue is recorded.
- Prevents duplicate processing when the button is clicked repeatedly.

The mod processes the order snapshot that exists when **Ship All** is clicked.
Orders created afterward wait for the next click.

## Requirements

- A legitimate Windows copy of *Nobody - The Turnaround*.
- BepInEx 6 IL2CPP. Version `6.0.0-be.791` was used for development and runtime
  testing.

Game updates can change IL2CPP types and break compatibility. Back up your save
before installing any mod.

## Installation

1. Install BepInEx 6 IL2CPP in the game directory.
2. Launch the game once, then close it, so BepInEx can finish initialization.
3. Download the Nobody Order Automation release ZIP.
4. Extract the ZIP into the game directory. The DLL should end up at:

   `BepInEx/plugins/NobodyOrderAutomation.dll`

5. Launch the game normally. No script or launcher needs to remain open.

## Usage

1. Enter the Warehouse.
2. Click **Ship All** beside **ESC QUIT**.
3. Fulfillable orders are shipped immediately. Insufficient-stock orders remain
   in the queue.

## Uninstallation

Close the game and delete:

`BepInEx/plugins/NobodyOrderAutomation.dll`

The mod does not install a service or create a separate background application.

## Building from source

Install the .NET 8 SDK, clone this repository, and point the build at your own
game installation after BepInEx has generated its IL2CPP interop assemblies:

```powershell
$env:NOBODY_GAME_DIR = 'X:\Path\To\Nobody'
.\build.ps1
```

Create a release archive with:

```powershell
.\package.ps1
```

The plugin targets .NET 6 for BepInEx compatibility. The dependency-free test
harness targets .NET 8. Game assemblies are referenced only from your local
installation and are never copied into the release package.

## Compatibility and support

The current release was tested with the Windows 64-bit IL2CPP version of the
game and BepInEx `6.0.0-be.791`. If the mod stops loading after a game update,
remove the DLL and check the repository's Issues page before reporting it.

For a useful bug report, include the game, BepInEx, and mod versions; concise
reproduction steps; and relevant BepInEx log lines with private information
removed. See [CONTRIBUTING.md](CONTRIBUTING.md) for the full checklist.

## Source, license, and third parties

The original code and documentation in this repository are available under the
[MIT License](LICENSE). You may inspect, use, modify, and redistribute them under
that license.

BepInEx, the game, Unity, and their assets are not included and are not covered
by this project's MIT License. See
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for details.

## Legal notice

This is an unofficial community mod. It is not affiliated with, endorsed by, or
supported by the developers or publishers of *Nobody - The Turnaround*, Unity,
or BepInEx. All third-party names, trademarks, assets, and copyrights belong to
their respective owners.

This repository and its release archive contain no game executable, game asset,
game assembly, BepInEx binary, or generated interop assembly. Users must obtain
the game and required runtime separately and comply with the terms that apply to
them. The software is provided as-is, without warranty, as stated in the MIT
License.
