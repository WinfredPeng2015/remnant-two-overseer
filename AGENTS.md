# Remnant Overseer Project Notes

## Scope

- This repository is the user's Traditional Chinese fork of Remnant Overseer.
- Keep UI text and item names localized in Traditional Chinese.
- Keep original acquisition hint text. Previous attempts to automatically shorten item hints lost important steps and were intentionally reverted.
- Do not assume an external translation MOD is available. The canonical translations currently live in this repository's RESX files.

## Paths

- Workspace root: `C:\Users\raido\OneDrive\文件\Remnant2`
- Repository root: `C:\Users\raido\OneDrive\文件\Remnant2\remnant-two-overseer`
- Solution: `C:\Users\raido\OneDrive\文件\Remnant2\remnant-two-overseer\RemnantOverseer.sln`
- Project: `C:\Users\raido\OneDrive\文件\Remnant2\remnant-two-overseer\RemnantOverseer\RemnantOverseer.csproj`
- Debug executable: `C:\Users\raido\OneDrive\文件\Remnant2\remnant-two-overseer\RemnantOverseer\bin\Debug\net10.0\RemnantOverseer.exe`
- Desktop development shortcut: `C:\Users\raido\OneDrive\桌面\Remnant Overseer - Dev.lnk`
- Release artifacts: `C:\Users\raido\OneDrive\文件\Remnant2\remnant-two-overseer\dist`
- Traditional Chinese UI strings: `RemnantOverseer\Resources\AppStrings.zh-Hant.resx`
- Traditional Chinese game and item strings: `RemnantOverseer\Resources\GameStrings.zh-Hant.resx`
- Debug and portable settings are stored as `settings.json` next to the running executable unless build constants change the settings mode.

The desktop shortcut must target the debug executable above. It intentionally follows the Debug output path, so a successful rebuild updates what the shortcut launches.

## Git

- Primary branch: `master` (this repository does not use a `main` branch).
- User fork remote: `origin` -> `git@github.com:WinfredPeng2015/remnant-two-overseer.git`
- Original project remote: `upstream` -> `https://github.com/Angelore/remnant-two-overseer.git`
- Always inspect `git status --short --branch` before editing, merging, or committing.
- `dist/` is intentionally untracked. Never stage or commit it unless the user explicitly requests release artifacts in Git.
- Do not revert unrelated user changes in a dirty worktree.

## Build And Run

Run from the repository root:

```powershell
dotnet restore RemnantOverseer.sln -p:Configuration=Debug
dotnet build RemnantOverseer.sln --no-restore
```

Launch:

```powershell
Start-Process -FilePath 'C:\Users\raido\OneDrive\文件\Remnant2\remnant-two-overseer\RemnantOverseer\bin\Debug\net10.0\RemnantOverseer.exe'
```

Before rebuilding, it is okay to close the development build without asking again, but only after matching the process executable path exactly to the Debug executable above. Do not terminate other Remnant Overseer installations.

Avalonia may need to write build telemetry under `%LOCALAPPDATA%\AvaloniaUI\BuildServices`; a sandboxed build can fail there with `UnauthorizedAccessException`. In that case rerun the ordinary `dotnet build` with the required filesystem permission.

## Current Custom Features

- Traditional Chinese culture is `zh-Hant`.
- Font fallbacks in `App.axaml` include Microsoft JhengHei UI so Traditional Chinese glyphs render correctly.
- World Items can use a four-level tree: `Zone -> Location -> SubLocation -> Item`.
- A `SubLocation` represents a named analyzer `overworld POI` loot group nested under the rolled parent `Location`; do not infer or hard-code its parent from the POI name.
- Treat a sublocation as a World Stone only when its localized POI name exactly matches one of the save's canonical waypoint names. Known examples include `Morrow Parish -> Oracle's Refuge` and `The Eon Vault -> Extraction Hub`.
- Keep a POI's items directly under the parent location when the POI name is empty or equals the parent location name, so the tree does not show redundant paths such as `Lemark District -> Lemark District`.
- Search, category filters, acquired-item filters, and localization refreshes must traverse both direct `Location.Items` and nested `SubLocation.Items`.
- Vendor, boss, dungeon, injectable, world-drop, and other non-POI item groups remain direct children of their parent `Location`.
- Current-world quest items bypass the permanent-profile duplicate check in `Utilities\DatasetMapper.cs`. Their visibility is controlled by their actual `IsLooted` state.
- Item name colors are applied in both World and Missing Items views through `Utilities\ItemTypeToForegroundConverter.cs`.
- Item color preferences are persisted in `settings.json`.
- The Settings page provides Original, Preset, and Custom modes for every concrete item type, plus an Avalonia ColorPicker and HEX input.
- Preset colors are defined by `Services\ItemColorService.cs`.
- Selected color-mode buttons are ordinary Buttons with an explicit `selected` class in `Views\SettingsView.axaml`. Do not replace them with independently toggleable ToggleButtons; a selected mode must not be clearable.

## Validation

- Build must complete with zero errors before committing.
- Run `git diff --check`; CRLF conversion warnings are expected on Windows.
- Launch the exact Debug executable after UI changes.
- For World Items sublocation changes, verify that moving a POI group does not hide or duplicate items, self-named POIs remain direct items, nested items respond to every filter/search mode, language changes refresh nested labels, and exact waypoint matches show the World Stone marker.
- For color-setting changes, verify:
  - exactly one mode has the gray selected background;
  - clicking the selected mode does not clear it;
  - selecting Custom reveals the picker and HEX field;
  - changing a custom color updates item names immediately;
  - disabled or looted items remain gray.

## Safety

- Do not use PowerShell reflection, dynamic assembly loading, or scripted method invocation against the built DLL. Windows Defender previously flagged that workflow.
- Prefer normal `dotnet restore`, `dotnet build`, `dotnet publish`, direct executable launch, and focused source-level tests.
- Keep release output out of Git unless explicitly requested.
