# UiKit - native UI for Dice Kingdoms mods

A small library for BepInEx 6 (IL2CPP) mods. Everything it shows is a **clone of the game's own UI**: the same buttons, sprites, fonts, tooltips,
sounds and cursor changes, so mod UI looks and behaves like the game. Several mods can use it side by side.

| You want | Use |
|---|---|
| A window with buttons, labels, toggles, layouts | `UiKit.Window(...)` |
| A button in the top-right bar (next to Rules / Settings) | `UiKit.TopBar.Add(...)` |
| Rows in the game's Settings screen | `UiKit.Settings.Add(...)` |

## Setup

1. Link the game's interop assemblies once: `scripts\link-interop.ps1 "E:\SteamLibrary\steamapps\common\Dice-Kingdoms"` (creates `interop/`, which is git-ignored).
2. `dotnet build -c Release`, copy `DiceModders.UiKit.dll` to `BepInEx/plugins`.
3. In your mod: reference `DiceModders.UiKit.dll` and add `[BepInDependency("<UiKit plugin guid>")]` (the guid is `MyPluginInfo.PLUGIN_GUID`, generated from the assembly name).
4. Demo content (windows, a top bar button, a "UiKit demo" settings block) is controlled by `BepInEx/config/*UiKit*.cfg` -> `[Demo] Enabled`. It is off by default; set it to `true` to see the demo.

## For mod authors / Thunderstore

UiKit is its own BepInEx plugin. Publish it as a separate Thunderstore package and list it in your mod's `manifest.json` `dependencies`; mod managers then install it
automatically. In code add `[BepInDependency(UiKit plugin GUID)]` so UiKit loads first. Do not copy the DLL into your own package (two copies would both patch the game). A complete example is in [docs/EXAMPLE_MOD.md](docs/EXAMPLE_MOD.md).

## Windows

```csharp
UiKit.Window(w => {
    w.Size(360, 240).Draggable();
    int hp = 100;
    var label = w.Body.Label($"HP: {hp}");
    w.Body.Button("+10", () => { hp += 10; label.SetText($"HP: {hp}"); });
    w.Body.Toggle("God mode", false, on => Log(on));
    w.Body.Horizontal(row => {
        row.IconButton("hammer", () => Log("hi"), "Tooltip text").FixedW(32);
        row.Button("Close", () => w.Close());
    }).FixedH(32);
});
```

Layout (`Vertical`, `Horizontal`, `Grid`, `Fixed*`, `Weight*`, `Spacing`, `Padding`) is unchanged from earlier versions. Buttons are 32 high; `Button(...)` defaults to that.
`ButtonHandle` also has `OnClick`, `SetSelected(bool)` (the game's selected/unselected art), `SetIcon`, `SetTooltip`.
Icon names are the game's sprite names; list them with `Assets.SpriteNames("filter")`.

## Top bar

```csharp
UiKit.TopBar.Add("mymod.panel", "hammer", () => ShowPanel(), "My panel", order: 10);
```

Ids must be unique per mod (prefix them with your mod name). Buttons are placed left of the game's own buttons, sorted by `order`, then registration,
and re-attach themselves after scene changes. The returned `TopBarButton` has `SetIcon`, `SetTooltip`, `SetVisible`, `SetInteractable`, `Remove`.

## Settings rows

```csharp
UiKit.Settings.Add(SettingsTab.Game, "My Mod", s => {
    s.Toggle("Enabled", () => cfg.Enabled, v => cfg.Enabled = v);
    s.Stepper("Speed", () => cfg.Speed.ToString("0.0"), d => cfg.Speed += d * 0.5f);
    s.Button("Reset", "Reset", () => cfg.Reset());
    s.Info("Changes apply immediately.");
});
```

The callback runs each time the game rebuilds that tab (a postfix on `SettingSetInterface.GenerateElements`), so no scanning is needed. Toggle and
stepper rows refresh while the screen is open. Tabs: `Video`, `Audio`, `Input`, `Game`.

## How it stays native

- Buttons: clone of the Rules button (icons) and `Settings/Close Button` (text), under an inactive parent so `Awake` only runs when configured. Clicks go
  through the game's `BaseButton.onClick`, tooltips through `HasToolTip`; the donor's listeners are removed.
- Text: font asset **and** material copied together from a live game text.
- Never clear a clone's sprite (`BaseButton.Update` would throw every frame). Sliced art needs size ~ 2x border, hence 31.75 instead of 32.
- Checkbox: clone of the game's boolean setting control with its settings binding removed.
- Everything the library creates is named `DM ...`, so mods can tell it apart.

## Troubleshooting

Look in `BepInEx/LogOutput.log` for lines from UiKit. `native text button template not found` means the game screen with the template was not loaded yet; the
library retries and falls back to its older hand-built widgets. Report issues with the log attached.
