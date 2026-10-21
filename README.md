# UiKit - native UI for Dice Kingdoms mods

A small library for BepInEx 6 (IL2CPP) mods. Everything it shows is a **clone of the game's own UI**: the same buttons, sprites, fonts, tooltips,
sounds and cursor changes, so mod UI looks and behaves like the game. Several mods can use it side by side.

UiKit is only the **interface**: one entry point (one canvas, one asset cache, one API) for adding to the game's screens. Game and networking logic (the Steam lobby, lobby rules,
civilizations) is [ModCore](https://github.com/DiceModders/DiceKingdoms-ModCore); the small [ModBridge](https://github.com/DiceModders/DiceKingdoms-ModBridge) plugin shows ModCore's data in UiKit's screens. The libraries do not depend on each other.

| You want | Use |
|---|---|
| A window with buttons, labels, toggles, layouts | `UiKit.Window(...)` |
| A button in the top-right bar (next to Rules / Settings) | `UiKit.TopBar.Add(...)` |
| Rows in the game's Settings screen | `UiKit.Settings.Add(...)` |

## Install (players)

Copy `DiceModders.UiKit.dll` into `Dice-Kingdoms\BepInEx\plugins`. Mods that use UiKit list it as a dependency, so mod managers install it for you.

## Build from source

Requirements: .NET SDK 6 or newer, the game installed with BepInEx 6 (IL2CPP) and started once (this generates `BepInEx\interop`).

```powershell
$game = "<your Dice-Kingdoms folder>"   # the folder that contains BepInEx, e.g. ...\steamapps\common\Dice-Kingdoms (Steam: Properties > Installed Files > Browse)
git clone https://github.com/DiceModders/UIKit_Another_Version
cd UIKit_Another_Version
.\scripts\link-interop.ps1 $game    # creates .\interop (git-ignored)
dotnet build -c Release --no-incremental
Copy-Item bin\Release\net6.0\DiceModders.UiKit.dll "$game\BepInEx\plugins" -Force
```

Demo content (windows, a top bar button, a "UiKit demo" settings block) is controlled by `BepInEx/config/DiceModders.UiKit.cfg` -> `[Demo] Enabled` (off by default).
Debug helpers in the same file: `[Debug] ShowInvisible` (draws invisible hotspots), `[Debug] DumpSpriteNames` (writes `BepInEx/uikit-sprites.txt`, the list of icon names).

## Write a mod with it

1. Follow **[docs/EXAMPLE_MOD.md](docs/EXAMPLE_MOD.md)** (a complete minimal plugin) or the longer tutorial in ModBridge's `docs/FIRST_MOD.md` (all three libraries).
2. Reference `DiceModders.UiKit.dll` with `Private=false`, and add `[BepInDependency("DiceModders.UiKit")]` to your plugin.
3. **Name clash:** the namespace `DiceModders.UiKit` contains a class `UiKit`. Inside a namespace that starts with `DiceModders.` write
   `using Ui = DiceModders.UiKit.UiKit;` and call `Ui.Window(...)`, `Ui.TopBar...`; elsewhere `using DiceModders.UiKit;` and `UiKit.Window(...)` work directly.
4. Ids (top bar, settings blocks, hotspots, scoreboard columns) are shared by all mods: prefix them with your name.
5. `UiKit.Window(...)` needs the game's UI to exist, so call it from a click handler, not from `Load()`. Registrations (`TopBar.Add`, `Settings.Add`, ...) may happen in `Load()`.
6. Find icon names with `[Debug] DumpSpriteNames = true` (see above) or `Assets.SpriteNames("filter")`.

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
        row.IconButton("hammer", () => Log("hi"), "Tooltip text");
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
    w.Size(420, 520).Draggable().Closable();                    // Closable(): the game's own X button, top right, in its own header row

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
An image that has an `OnScroll` handler takes the wheel before a scroll view around it.

### Wrapping text and a title bar

```csharp
UiKit.Window(w => {
    w.Size(360, 520).Draggable().Fullscreenable().Closable().Title("Island Editor");   // title strip; Closable/Title in any order
    w.TitleBar.Button("Help", () => ShowHelp());                    // buttons on the right of the strip (as wide as their text)
    w.TitleBar.IconButton("hammer", Tools, "Tools");
    w.FullscreenButton();                                           // optional "Full screen / Window" toggle in the strip

    var text = w.Body.Paragraph(longText);                          // wraps at the width it gets, height follows the text
    text.SetText(otherText);                                        // re-measures
    w.Body.Label("Plain label").Wrap();                             // Label(...) is one fixed line unless you call Wrap() / AutoHeight()
    w.Body.ScrollView(sv => sv.Paragraph(longText)).WeightH(1);     // also fine inside scroll views
});
```

`Paragraph(text, fontSize = 16)` returns a `LabelHandle`; on it `Wrap(bool on = true)`, `AutoHeight(bool on = true)`, `Align(TextAlignmentOptions)`, `Left()`, `Center()`, `Right()`.
A window body cuts content that does not fit; put long or changing text into a `ScrollView`. A scroll view stays at the top until the user scrolls, also while its content grows.
A title that is too long is cut with "..." before it reaches the buttons; with `Closable()` the strip also keeps clear of the X.

`Closable()` always gives the X its own header row (about 48 px) above the body, so content never runs under it; `Title` and `TitleBar` use the same row, so nothing is reserved twice.
A window without `Closable`, `Title` and `TitleBar` has no header row and uses the full height. Icon and text buttons are the same height (32), so they line up in the title bar.
A plain `Label` is one line and is cut with "..." when it is too long (use `Paragraph` or `.Wrap()` to wrap).

## Rules screen

```csharp
// an entry in the civilization drop-down of the Rules screen; picking it selects that civilization number
UiKit.Rules.AddCivilizationEntry(civilizationId, "My civilization");
UiKit.Rules.Opened += screen => { /* the Rules screen was shown */ };
```

The game fills the drop-down from its `Civilization` enum when the screen wakes up; `AddCivilizationEntry` appends an entry to it (a postfix on `Rules.Awake`/`OnEnable`).
That is all UiKit does: registering a civilization (its id, tech tree, skins) is game logic and lives in ModCore; the ModBridge plugin adds the drop-down entries for the civilizations ModCore knows.

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
stepper rows refresh while the screen is open.

A number between two limits is best shown with the game's own slider (the control of its Frame Rate Limit setting):

```csharp
s.Slider("Speed", min: 1, max: 5, get: () => speed, set: v => speed = (int)v, wholeNumbers: true, format: "0");
// optional last argument: new SettingsRowOptions { Enabled = () => canEdit, Visible = () => shown }
```

The slider fits the width of the column it is in (it is laid out again when the real width is known), so it never runs under the scrollbar.

Tabs: `Video`, `Audio`, `Input`, `Game` and `Lobby` (the settings box of the match-start screen).

Rows can be disabled or hidden while the screen is open (both callbacks are re-read four times a second):

```csharp
var opts = new SettingsRowOptions { Enabled = () => isHost, Visible = () => unlocked };
s.Toggle("Meteors", () => on, v => on = v, opts);
s.Stepper("Speed", () => speed.ToString(), d => speed += d, opts);
s.Info(() => warningTextOrEmpty);    // live paragraph, hidden while empty
```

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

Look in `BepInEx/LogOutput.log` for lines from UiKit. "template was not found" errors mean the game screen that holds the template was not loaded yet when the
widget was created: create windows after the game's UI is up (for example from a button press). Report issues with the log attached.

## Project layout

| Path | What is in it |
|---|---|
| `Plugin.cs`, `Core.cs`, `UiKit.cs` | Entry point, one-time installation, the public facade (`UiKit.Window`, `.TopBar`, `.Settings`, `.Rules`, `.Scoreboard`). |
| `Window.cs`, `UiFactory.cs`, `UiRoot.cs`, `Handles.cs`, `ContainerHandle.cs`, `Widgets/` | The window system: one shared canvas, layout containers and the handles you get back (label, button, toggle, image, slider, dropdown, scroll view). |
| `Native/` | Everything that clones the game's own UI: buttons, toggles, text, frames, icon sprites, templates, the per-frame runner, logging. |
| `Assets.cs` | Asset cache: game sprites by name, textures, `SpriteNames(filter)`, the sprite name dump. |
| `TopBar/` | The top-right button bar. |
| `Settings/` | Rows in the game's Settings screen and the lobby settings box (`SettingsSection` is the API, `SettingsBuilder` builds the rows, `SettingsLive` refreshes them, `SettingsHotspots` the hidden click spots). |
| `Rules/` | Appending entries to the Rules screen's civilization drop-down. |
| `Scoreboard/` | Extra scoreboard columns. |
| `Lobby/` | Comment-only stubs; the lobby moved to ModCore (delete this folder in your checkout). |
| `docs/NOTES.md` | Implementation notes (why things are done the way they are). Read before touching `Native/`. |
| `docs/EXAMPLE_MOD.md` | A complete minimal mod. |

## Contributing

- UiKit is **only the interface**. No lobby, Steam, rules or civilization logic here; that is ModCore. Code that needs both goes into ModBridge.
- Every widget is a clone of the game's own, built under an inactive parent; follow `docs/NOTES.md` ("How it stays native") for new ones.
- Add public overloads instead of optional parameters to existing methods (compiled mods call the old signature).
- Wrap patches and per-frame code in `try/catch` and log with `UiLog.Once(key, message)`; a per-frame error must never print every frame.
- After a change, test with the demo (`[Demo] Enabled = true`) and in the lobby, the Settings screen and a running match.
