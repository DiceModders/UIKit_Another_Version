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
