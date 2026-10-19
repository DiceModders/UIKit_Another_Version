using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace DiceModders.UiKit;

[HarmonyPatch(typeof(DiceKingdoms.Meta.MainMenu), nameof(DiceKingdoms.Meta.MainMenu.Start))]
public static class InterfaceAwakePatch {
    [HarmonyPostfix]
    public static void Postfix() {
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
		}).Draggable().Size(300,300);

    }
}


[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin {
    internal static new ManualLogSource Log;

    public override void Load() {
        Log = base.Log;
		ClassInjector.RegisterTypeInIl2Cpp<WindowDragHandler>();
		var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(InterfaceAwakePatch));
    }
}
