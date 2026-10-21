using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace DiceModders.UiKit;

[HarmonyPatch(typeof(DiceKingdoms.Meta.MainMenu), nameof(DiceKingdoms.Meta.MainMenu.Start))]
public static class InterfaceAwakePatch {
    [HarmonyPostfix]
    public static void Postfix() {
		if (Plugin.DumpWindows.Value) WindowProbe.Dump();
		if (!Plugin.DemoEnabled.Value) return;
		Assets.Scan();
		if (UiRoot.Root==null) {
			Plugin.Log.LogError("No canvas");
		}

		var w1 = UiKit.Window(w => {
			w.Size(360, 240) .Draggable();

			var body = w.Body;

			int hp = 100;
			var hpLabel = body.Label($"HP: {hp}");
			body.Button("+10", () => {hp+=10; hpLabel.SetText($"HP: {hp}");});
			body.Button("-10", () => {hp-=10; hpLabel.SetText($"HP: {hp}");});
		});

		var w2 = UiKit.Window(w => {
			w.Size(360, 240).Draggable().Fullscreenable();

			w.Body.Label("Test minimize / fullscreen");
			w.Body.Button("Fullscreen", () => w.Fullscreen());
		});

		UiKit.Window(w => {
            w.Size(340, 340).Position(80, 80).Draggable() .Ornament(Assets.OrnamentStyle.None);

            var body = w.Body;
            var currentLabel = body.Label("Current: Ornament1");

            var styles = new[] {
				Assets.OrnamentStyle.None, 
				Assets.OrnamentStyle.Border, 
				Assets.OrnamentStyle.BorderWithTwoCorners, 
				Assets.OrnamentStyle.BorderWithFourCorners
            };

            foreach (var style in styles) {
                var s = style;
                body.Button(style.ToString(), () => {
                    w.Ornament(s);
                    currentLabel.SetText($"Current: {s}");
                });
            }
        });

		UiKit.Window(w => {
			w.Size(400, 320).Position(80, 80).Draggable();

			var body = w.Body;
			body.Label("Settings").FixedH(24);

			body.Vertical(v => {
				v.Label("Option A");
				v.Label("Option B");
				v.Label("Option C");
			}).WeightH(1).Spacing(4);

			body.Horizontal(row => {
				row.Button("Apply",  null);
				row.Button("Cancel", null);
			}).FixedH(32);
		}).Draggable();

		UiKit.Window(w => {
			w.Body.Horizontal(h => {
				h.Vertical(left => {
					left.Label("Menu");
					left.Button("Home", null);
					left.Button("Settings", null);
				}).FixedW(140).WeightH(1);

				h.Vertical(right => {
					right.Label("Content area");
				}).WeightW(1).WeightH(1);
			}).WeightH(1);
		}).Draggable();

		UiKit.Window(w => {
			w.Body.Grid(g => {
				for (int i = 0; i < 9; i++)
					g.Button($"{i}", null);
			}).Columns(3).CellSize(80, 80).WeightH(1);
		}).Draggable().Size(284,284);   // 3 cells of 80 + 2 gaps of 6 + 2 * 16 padding

		UiKit.Window(w => {
			w.Size(360, 200).Position(480, 80).Draggable();
			var status = w.Body.Label("Native widgets");
			w.Body.Toggle("Native checkbox", false, on => status.SetText($"Checkbox: {on}"));
			w.Body.Horizontal(row => {
				row.IconButton("hammer", () => status.SetText("Icon button"), "Native icon button");
				row.Button("Text button", () => status.SetText("Text button"));
			}).FixedH(32);
		});
		UiKit.Window(w => {
			w.Size(420, 560).Position(520, 60).Draggable().Closable().Title("Painting");
			var b = w.Body;
			var status = b.Label("Move over / drag on the picture, wheel changes the brush");

			const int N = 64;
			var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
			for (int y = 0; y < N; y++)
				for (int x = 0; x < N; x++)
					tex.SetPixel(x, y, new Color(x / (float)N, y / (float)N, 0.35f, 1f));
			tex.Apply(false);

			int brush = 2;
			// A picture is stretched to its rectangle, so give it a size with the texture's proportions: a horizontal row keeps it from being widened.
			ImageHandle img = null;
			b.Horizontal(row => { img = row.Image(tex).Filter(FilterMode.Point).Refresh().Fixed(150); }).FixedH(150);
			img.OnPointer(e => {
				int px = Mathf.Clamp((int)(e.Uv.x * N), 0, N - 1), py = Mathf.Clamp((int)(e.Uv.y * N), 0, N - 1);
				if ((e.Kind == PointerKind.Down || e.Kind == PointerKind.Drag) && e.Button == 0) {
					for (int dy = -brush; dy <= brush; dy++)
						for (int dx = -brush; dx <= brush; dx++) {
							int x = px + dx, y = py + dy;
							if (x >= 0 && y >= 0 && x < N && y < N) tex.SetPixel(x, y, Color.white);
						}
					img.Refresh();
				}
				if (e.Kind != PointerKind.Exit) status.SetText($"{e.Kind} button {e.Button}  pixel {px},{py}  brush {brush}");
			});
			img.OnScroll(n => { brush = Mathf.Clamp(brush + (n > 0 ? 1 : -1), 1, 8); status.SetText($"brush {brush}"); });

			b.Slider(0, 100, 40, v => status.SetText($"Slider {v:0}"), "0");
			b.Dropdown(new[] { "First", "Second", "Third" }, 1, i => status.SetText($"Dropdown index {i}")).FixedH(32);
			b.ScrollView(sv => {
				for (int i = 1; i <= 20; i++) sv.Label($"Scrolling row {i}");
			}).WeightH(1);   // fills what is left of the window
		});

		// Wrapping text and the title bar: a ~600 character paragraph, one inside a scroll view, one whose text is replaced by a much longer one.
		const string Lorem = "Dice Kingdoms is a turn based strategy game where every civilization grows from a handful of tiles into a trading, building and fighting empire. " +
			"This paragraph wraps at the width it gets: it is not given a pixel width by the caller and its height follows the wrapped text, so nothing runs out of the window and " +
			"no empty gap is left below it. Resize the window with the Full screen button, or press the button below to swap in a text that is several times longer, and " +
			"watch the height follow. The same paragraph is repeated inside the scroll view at the bottom, where it must keep the scrollbar in step with it.";
		UiKit.Window(w => {
			w.Size(360, 640).Position(980, 60).Draggable().Fullscreenable()
				.Title("Title bar with a very long title that has to be cut with dots").Closable();   // Closable after Title; the order does not matter
			w.TitleBar.Button("Hi", () => Plugin.Log.LogInfo("title bar button"));
			w.FullscreenButton();

			w.Body.Paragraph(Lorem);
			// The text below is replaced by one three times as long. It sits in a scroll view: a fixed-size window cannot grow with its text,
			// so long, changing text belongs in a ScrollView (anything that does not fit a window body is cut at the body, never drawn outside).
			LabelHandle swap = null;
			bool longer = false;
			w.Body.Button("Swap text", () => { longer = !longer; swap.SetText(longer ? Lorem + " " + Lorem + " " + Lorem : Lorem); });
			w.Body.ScrollView(sv => { swap = sv.Paragraph(Lorem); sv.Paragraph(Lorem, 14f); }).WeightH(1);
		});
		UiKit.Window(w => {
			w.Size(300, 250).Position(980, 720).Draggable().Closable().Title("Closable first");   // Closable before Title
			w.TitleBar.IconButton("hammer", () => Plugin.Log.LogInfo("icon in title bar"), "Title bar icon");
			w.Body.Paragraph("A label can also wrap: Label(...).Wrap() turns word wrapping and auto height on.");
			w.Body.Label("Wrapped label with Wrap(): a few more words so that it needs a second line in this narrow window.").Wrap();
		});
    }
	
	internal static void RegisterDemoExtras() {
		UiKit.TopBar.Add("uikit.demo", "hammer", () => Plugin.Log.LogInfo("UiKit demo top bar button clicked"), "UiKit demo button");

		bool flag = false;
		int level = 5;
		UiKit.Settings.Add(SettingsTab.Game, "UiKit demo", s => {
			s.Toggle("Demo toggle", () => flag, v => flag = v);
			s.Stepper("Demo level", () => level.ToString(), d => level = System.Math.Clamp(level + d, 0, 10));
			s.Button("Demo action", "Run", () => Plugin.Log.LogInfo("UiKit demo settings button clicked"));
			s.Info("Rows above are clones of the game's own settings rows.");
			for (int i = 1; i <= Plugin.DemoExtraRows.Value; i++) {
				int n = i;
				bool on = false;   // each row has its own state
				s.Toggle($"Extra row {n}", () => on, v => on = v);
			}
		});

		if (Plugin.DemoRulesScoreboard.Value) {
			// A scoreboard column that shows the kingdom's name.
			UiKit.Scoreboard.AddColumn("uikit.demo", island => island != null ? island.name : "-", "UiKit demo column");
		}
	}
}


[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin {
    internal static new ManualLogSource Log;
    internal static BepInEx.Configuration.ConfigEntry<bool> DemoEnabled;
    internal static BepInEx.Configuration.ConfigEntry<int> DemoExtraRows;
    internal static BepInEx.Configuration.ConfigEntry<bool> DumpWindows;
    internal static BepInEx.Configuration.ConfigEntry<bool> ShowInvisible;
    internal static BepInEx.Configuration.ConfigEntry<bool> DumpSpriteNames;
    internal static BepInEx.Configuration.ConfigEntry<bool> DemoRulesScoreboard;

    public override void Load() {
        Log = base.Log;
		DemoEnabled = Config.Bind("Demo", "Enabled", false, "Show the demo windows, top bar button and settings rows (for testing the library; off by default).");
		DemoExtraRows = Config.Bind("Demo", "ExtraRows", 0, "Adds this many extra toggle rows to the demo settings block (to test scrolling and the Close button shift).");
		DemoRulesScoreboard = Config.Bind("Demo", "Scoreboard", false, "With the demo on: adds a scoreboard column.");
		DumpWindows = Config.Bind("Debug", "DumpWindows", false, "Writes the object tree of the game's own windows to the log (for developers).");
		ShowInvisible = Config.Bind("Debug", "ShowInvisible", false, "Draws the invisible things UiKit adds (hidden click spots in red, the scroll catcher in green) so you can see where they are. Takes effect the next time a screen is opened.");
		DumpSpriteNames = Config.Bind("Debug", "DumpSpriteNames", false, "Writes the names of all of the game's sprites (the names the icon parameters take) to BepInEx/uikit-sprites.txt the first time they are read.");
		UiKit.Init();
		if (DemoEnabled.Value) InterfaceAwakePatch.RegisterDemoExtras();
		var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(InterfaceAwakePatch));
    }
}
