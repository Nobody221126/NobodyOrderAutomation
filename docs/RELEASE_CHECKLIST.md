# Release checklist

- [ ] Unit tests report 11/11 passed.
- [ ] Release build completes with zero warnings and zero errors.
- [ ] Repository scan contains no saves, logs, credentials, absolute local paths, game assemblies, generated interop assemblies, or BepInEx binaries.
- [ ] GitHub identifies the root `LICENSE` as MIT.
- [ ] Third-party notices still match the dependencies actually used by the mod.
- [ ] Nexus ZIP contains only the plugin DLL, README, changelog, MIT license, and third-party notices.
- [ ] Install the ZIP contents into the clean test copy.
- [ ] Confirm the plugin loads automatically.
- [ ] Confirm Ship All is visible beside ESC QUIT.
- [ ] Confirm a large batch ships fulfillable orders and preserves insufficient-stock orders.
- [ ] Confirm inventory and the order list refresh immediately.
- [ ] Confirm packing animation and in-game time advancement are skipped.
- [ ] Confirm online-shop revenue is recorded after day rollover.
- [ ] Confirm the GitHub repository is public and the source link in the Nexus description works.
- [ ] Add Nexus screenshots and upload the verified ZIP.

