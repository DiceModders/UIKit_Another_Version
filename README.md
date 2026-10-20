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

### More elements

```csharp
UiKit.Window(w => {
    w.Size(420, 520).Draggable().Closable();                    // Closable(): the game's own X button, top right

    var map = w.Body.Image(texture).Filter(FilterMode.Point).FixedH(200);   // a RawImage; Refresh() after drawing into the texture
    map.OnPointer(e => {                                                     // Down, Up, Click, Move, Drag, Enter, Exit
        if (e.Kind == PointerKind.Drag && e.Button == 1) Pan(e.Delta);       // e.Uv is 0..1 inside the picture, e.Position/Delta are canvas units
    });
    map.OnScroll(notches => Zoom(notches));                                  // mouse wheel over the picture

    w.Body.Slider(0, 100, 40, v => Log(v), "0");                              // game slider + value text; SetValue/SetRange/WholeNumbers/Format
    w.Body.Dropdown(new[] { "A", "B", "C" }, 0, i => Log(i));                 // game dropdown; SetIndex/SetOptions
    w.Body.ScrollView(list => { for (int i = 0; i < 30; i++) list.Label("row " + i); }).FixedH(120);
});
```

Windows are not draggable unless you call `Draggable()`, so a "locked" window is simply one without it.
The mouse wheel is read directly from the Input System (only while the pointer is over an image or scroll view), because the game's UI does not deliver wheel events reliably.
A picture is stretched to its rectangle: for square pixels give it a size with the texture's proportions, inside a `Horizontal` row so it is not widened (`row.Image(tex).Fixed(150)`).
An image that has an `OnScroll` handler takes the wheel before a scroll view around it.

## Rules screen (civilizations)

```csharp
UiKit.Rules.AddCivilization(10, "My civilization", () => myTechTree);   // Func<DiceKingdoms.Game.TechTree>

// Several mods adding civilizations? Let UiKit choose the number so they never collide
// (same key = same id on every machine, 1000 and up). Use civ.Id for your own data.
var civ = UiKit.Rules.AddCivilization("authorname.mycivilization", "My civilization", () => myTechTree, skinsLike: 2);
UiKit.Rules.Opened += screen => { /* the Rules screen was shown */ };
```

The game fills the civilization drop-down of the Rules screen from its `Civilization` enum and looks tech trees up in its own list (it throws for an unknown civilization).
`AddCivilization` appends your entry to the drop-down, so picking it shows that civilization's tree on the Technologies tab, and returns your tree whenever the game asks
for the tree of that number (Rules screen and match). Your mod still has to provide the civilization's own data (skins, colours, the `TechTree` asset) - see `docs/NOTES.md`.

## Scoreboard

```csharp
UiKit.Scoreboard.AddColumn("mymod.gold", island => GoldOf(island).ToString(), "Gold income");
```

Adds one cell to every row of the match scoreboard, copied from the game's own stat cells and refreshed four times a second.

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
stepper rows refresh while the screen is open. Tabs: `Video`, `Audio`, `Input`, `Game` and `Lobby` (the settings box of the match-start screen).

```csharp
UiKit.Settings.Opened += screen => Log("Settings screen shown");
// hidden unlock: 5 quick clicks in the bottom-left corner of the Settings screen
UiKit.Settings.AddHotspot("mymod.unlock", SettingsCorner.BottomLeft, 5, () => Unlock());
```

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


### Pictures keep square pixels
`row.Image(texture)` keeps the texture's proportions in any container (centered in the space it gets), so a 64x64 canvas is always square. `KeepAspect(false)` stretches it instead.
