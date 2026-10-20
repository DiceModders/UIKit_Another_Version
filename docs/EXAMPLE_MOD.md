# Example mod using UiKit

A complete minimal plugin: a window, a top bar button and a settings block.

## `MyMod.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <LangVersion>latest</LangVersion>
    <RestoreAdditionalProjectSources>
      https://api.nuget.org/v3/index.json;
      https://nuget.bepinex.dev/v3/index.json;
      https://nuget.samboy.dev/v3/index.json
    </RestoreAdditionalProjectSources>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BepInEx.Unity.IL2CPP" Version="6.0.0-be.*" IncludeAssets="compile" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include="interop/*.dll"><Private>false</Private></Reference>
    <Reference Include="path/to/DiceModders.UiKit.dll"><Private>false</Private></Reference>
  </ItemGroup>
</Project>
```

## `Plugin.cs`

```csharp
using BepInEx;
using BepInEx.Unity.IL2CPP;
using DiceModders.UiKit;

[BepInPlugin("com.example.mymod", "My Mod", "1.0.0")]
[BepInDependency("DiceModders.UiKit")]   // UiKit loads first
public class MyModPlugin : BasePlugin {
    private bool _enabled = true;
    private WindowHandle _window;

    public override void Load() {
        // 1. A button in the top-right bar, next to the game's own buttons. Icon = name of a game sprite (Assets.SpriteNames("filter")).
        UiKit.TopBar.Add("mymod.panel", "hammer", TogglePanel, "My Mod", order: 10);

        // 2. Rows in Settings -> Game, built from the game's own row prefabs.
        UiKit.Settings.Add(SettingsTab.Game, "My Mod", s => {
            s.Toggle("Enabled", () => _enabled, v => _enabled = v);
            s.Button("Panel", "Open", TogglePanel);
        });
    }

    private void TogglePanel() {
        if (_window != null) { _window.Close(); _window = null; return; }
        _window = UiKit.Window(w => {
            w.Size(320, 160).Draggable();
            var label = w.Body.Label("Hello");
            w.Body.Button("Click me", () => label.SetText("Clicked"));
        });
    }
}
```

Notes:
- Call UiKit from `Load()` or later; registration before the game's screens exist is fine, buttons and rows appear when the screens do.
- Window calls (`UiKit.Window`) need the game's UI to exist: call them from a click handler or after the main menu has loaded, not from `Load()`.
- Prefix your top bar ids with your mod name; ids must be unique.

## Thunderstore `manifest.json` (excerpt)

```json
{
  "name": "MyMod",
  "version_number": "1.0.0",
  "dependencies": ["BepInEx-BepInExPack_IL2CPP-6.0.x", "DiceModders-UiKit-1.0.0"]
}
```

Use the real package names and versions shown on Thunderstore for the dependency strings.
