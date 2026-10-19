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
			w.Size(360, 240).Draggable().Fullscreenable().Ornament(2);

			w.Body.Label("Test minimize / fullscreen");
			w.Body.Button("Fullscreen", () => w.Fullscreen());
		});

		var w3 = UiKit.Window(w => {
			int ornament = 1;
			w.Size(280, 160).Draggable().Ornament(ornament);

			var ornamentLable = w.Body.Label($"Ornament: {ornament}");
			w.Body.Button("Previous ornament", () => {ornament-=1; w.Ornament(ornament); ornamentLable.SetText($"Ornament: {ornament}");});
			w.Body.Button("Next ornament", () => {ornament+=1; w.Ornament(ornament); ornamentLable.SetText($"Ornament: {ornament}");});
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
