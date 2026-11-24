# Nobody Order Automation

An open-source quality-of-life mod that adds a native-looking **Ship All** button
to the Warehouse in *Nobody - The Turnaround*.

## What it does

- Ships every order that currently has enough inventory with one click.
- Skips insufficient-stock orders without deleting them.
- Handles long order lists and orders outside the visible scroll area.
- Refreshes the order list and inventory after processing.
- Skips the packing animation and does not advance in-game time.
- Uses the normal delivered-order state so online-shop revenue is recorded.
- Loads automatically with the game; no external helper script is required.

## Requirements

- A legitimate Windows copy of *Nobody - The Turnaround*.
- BepInEx 6 IL2CPP. Build `6.0.0-be.791` was used for development and testing.

## Installation

1. Install BepInEx 6 IL2CPP and launch the game once.
2. Close the game.
3. Extract this archive into the game directory.
4. Confirm that the DLL is at
   `BepInEx/plugins/NobodyOrderAutomation.dll`.
5. Launch the game normally and open the Warehouse.

Back up your save before installing mods. Game updates can break IL2CPP mods.

## Source code and license

The source code is public at:
<https://github.com/Nobody221126/NobodyOrderAutomation>

The mod's original code and documentation are licensed under the MIT License.
The archive includes the license and third-party notices. It does not include
game files, BepInEx binaries, or generated interop assemblies.

## Unofficial project notice

This is an unofficial community mod and is not affiliated with, endorsed by, or
supported by the developers or publishers of *Nobody - The Turnaround*, Unity,
or BepInEx. Third-party names, trademarks, assets, and copyrights remain the
property of their respective owners.
