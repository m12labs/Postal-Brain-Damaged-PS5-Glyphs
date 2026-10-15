# PS5 Glyphs for POSTAL: Brain Damaged

A BepInEx 6 (IL2CPP) mod that replaces POSTAL: Brain Damaged's Xbox controller prompts with PlayStation 5 glyphs, drawn in the game's own hand-drawn style. It covers menus, menu legends, tutorials and in-game hints.

| Xbox | PS5 |
| --- | --- |
| A, B, X, Y | Cross, Circle, Square, Triangle |
| LB, RB | L1, R1 |
| LT, RT | L2, R2 |
| View | Create |

The stick (L/R), L3/R3 and D-pad icons already suit PlayStation, so they stay as drawn. The Menu icon is already the PS5 Options icon. Keyboard and mouse prompts are unchanged.

## Install
1. Install [BepInEx 6.0.0-be.788, Unity IL2CPP win-x64](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip) into the game folder. In Steam, right-click the game, then Manage > Browse local files.
2. Start the game once and let BepInEx finish its first-run setup, then quit.
3. Download `PS5Glyphs-<version>.zip` from [Releases](../../releases) and extract its `BepInEx` folder into the game folder, merging with the existing one.
4. Start the game. `BepInEx\LogOutput.log` should show `PS5 Glyphs <version> loaded`.

To uninstall, delete `BepInEx\plugins\PS5Glyphs`.

**Play with Steam Input on**, which is Steam's default for a DualSense. The game's prompts and default bindings assume an Xbox layout. Without Steam Input, a natively connected DualSense gets some bindings in the wrong place (for example Hook on the PS button). That's the game's behaviour, and the mod doesn't change it.

The mod works alongside the [PBDMP multiplayer mod](https://github.com/macery12/Postal-Brain-Damaged-Multiplayer).

## Config
The config file is `BepInEx\config\pbd.ps5glyphs.cfg`. It's created on first run.

| Key | Default | Meaning |
| --- | --- | --- |
| `General.Enabled` | true | Turns the glyph swap on or off. |
| `General.RenameFallbackText` | true | Uses PlayStation names when the game shows a button name instead of an icon (for example "Right Shoulder" becomes "R1"). |
| `Debug.DumpAtlas` | false | Writes the original icons and the game's input icon tables to `BepInEx\PS5Glyphs\dump`. Use it to remap after a game update. |

## How it works
- **One icon sheet covers most prompts.** The game draws controller prompts as TextMeshPro `<sprite=N>` tags from the `SpriteAsset_Buttons` atlas, through `InputStringManager`.
- **The plugin edits that sheet at runtime.** It reads the atlas and, in each Xbox slot, erases the label inside the game's button shape. It stamps the PS5 symbol from `assets/glyphs` in its place, then points the sprite assets and their materials at the result.
- **Menu legends are handled separately.** Prompts like "A Confirm" and "B Back" use their own `Input_Xbox_*` UI sprites instead of the atlas. Those get the same treatment, applied on scene changes and whenever a `MenuLegendComponent` is shown.
- **Only the mod's own art ships.** The release contains just the symbol overlays; the game's artwork is never included.

`assets/glyph-map.json` lists what gets replaced:
- **Numbered entries** map an atlas sprite index (the `N` in `<sprite=N>`) to an overlay name. The indices come from the game's joystick icon table, which `DumpAtlas` writes to `inputicons.json`.
- **The `sprites` section** maps legend sprite names to overlays.

## Building
Requirements:
- the .NET SDK (see `global.json`);
- the game, with BepInEx installed and run once so `BepInEx\interop` exists.

The scripts and the build assume the default Steam install path. To use another location, pass `-GameDir "<game folder>"` to the scripts or `-p:GameDir=...` to `dotnet build`.

| Script | Purpose |
| --- | --- |
| `scripts/Build.ps1` | Builds the plugin and stages it in `artifacts/PS5Glyphs`. |
| `scripts/Release.ps1` | Builds and packages `artifacts/PS5Glyphs-<version>.zip` with `INSTALL.txt`, the README and the license. Use `-SkipBuild` to reuse the last build. |
| `scripts/Install-Plugin.ps1` | Copies the build into the game. |
| `scripts/Uninstall-Plugin.ps1` | Removes the plugin from the game. |
| `scripts/make_symbols.py` | Redraws the symbol overlays. Run it with `python -I`. It needs Pillow and Courier New Bold. |

## License
[MIT](LICENSE). POSTAL: Brain Damaged and its artwork belong to their respective owners. This project isn't affiliated with them.
