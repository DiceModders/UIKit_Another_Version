using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Il2CppInterop.Runtime;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace DiceModders.UiKit;

public static class Assets {
    private static Dictionary<string, Sprite> sprites;
    private static Dictionary<string, Material> materials;
    private static Dictionary<string, TMP_FontAsset> fonts;
	private static bool scanned;

    public static void Scan() {
        sprites = new(); 
		materials = new(); 
		fonts = new();

		//Plugin.Log.LogInfo("Sprites:");
        foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.From(typeof(Sprite))))
            if (o?.TryCast<Sprite>() is { } s && !string.IsNullOrEmpty(s.name)) {
				//Plugin.Log.LogInfo($"\t{s.name}");
				sprites.TryAdd(s.name, s);
			}

		//Plugin.Log.LogInfo("Materials:");
        foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.From(typeof(Material))))
            if (o?.TryCast<Material>() is { } m && !string.IsNullOrEmpty(m.name)) {
				//Plugin.Log.LogInfo($"\t{m.name}");
				materials.TryAdd(m.name, m);
			}

		//Plugin.Log.LogInfo("Fonts:");
        foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.From(typeof(TMP_FontAsset))))
            if (o?.TryCast<TMP_FontAsset>() is { } f && !string.IsNullOrEmpty(f.name)) {
				//Plugin.Log.LogInfo($"\t{f.name}");
				fonts.TryAdd(f.name, f);
			}

		scanned = true;
    }

	private static void EnsureScanned() { if (!scanned) Scan(); }

	public static Sprite GetSprite(string name) {
        EnsureScanned();
        return name != null && sprites.TryGetValue(name, out var s) ? s : null;
    }

    public static Material GetMaterial(string name) {
        EnsureScanned();
        return name != null && materials.TryGetValue(name, out var m) ? m : null;
    }

    public static TMP_FontAsset GetFont(string name) {
        EnsureScanned();
        return name != null && fonts.TryGetValue(name, out var f) ? f : null;
    }

	public static TMP_FontAsset FontFromLive() {
		foreach (var t in Object.FindObjectsOfType<TMP_Text>())
			if (t != null && t.font != null && t.gameObject.activeInHierarchy)
				return t.font;
		return null;
	}

    public enum ButtonVariant { Regular = 1, Selected = 2 }
    public enum ButtonState { Ready, Hovered, Pressed, Disabled }

    public static Sprite ButtonBg(ButtonVariant v, ButtonState s) => GetSprite($"Button-{(int)v}-{s}");

    public static Sprite PanelBg() => GetSprite("Background-3") ?? GetSprite("Background-4");
    public static Sprite Ornament(int style = 3) => GetSprite($"Ornament-{style}");

    public static TMP_FontAsset Font => GetFont("Main-Font-Normal") ?? FontFromLive();
    public static Material FontShadowMaterial => GetMaterial("Main-Font-Normal-Shadow");
}

public static class UIRoot {
    private static Canvas root;
    public static bool HostIsGame { get; private set; }

	public static Canvas Root {
        get {
            if (IsAlive(root)) return root;
            return Acquire();
        }
    }

	private static bool IsAlive(Canvas c) {
        if (c == null) return false;
        var go = c.gameObject;
        return go != null && go.activeInHierarchy;
    }

	private static bool TryAnchorLive(out Canvas canvas) {
        canvas = null;
        try {
            foreach (var t in UnityEngine.Object.FindObjectsOfType<TMP_Text>()) {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                var c = t.GetComponentInParent<Canvas>();
                if (c == null || c.renderMode == RenderMode.WorldSpace) continue;
                canvas = c.rootCanvas;
                if (canvas != null) return true;
            } 
        }
        catch {}
        return false;
    }

	private static bool TryScan(out Canvas canvas) {
        canvas = null;
        try {
			Canvas[] all = Object.FindObjectsOfType<Canvas>(true);
            Canvas best = null;
            int bestScore = int.MinValue;
            foreach (var c in all) {
                if (c == null) continue;
                if (!c.gameObject.scene.IsValid()) continue;

                int score = 0;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) score += 1;
                if (c.isRootCanvas) score += 5;
                if (c.enabled) score += 1;
                if (c.gameObject.activeInHierarchy) score += 1;
                if (c.sortingOrder == 0) score += 1;
                if (c.sortingOrder > 100) score -= 5;

                if (score>bestScore) { bestScore = score; best = c; }
            }
            canvas = best;
            return canvas != null;
        }
        catch {}
		return false;
    }

	private static Canvas CreateOverlay() {
        var go = new GameObject("DiceModders UI");
        UnityEngine.Object.DontDestroyOnLoad(go);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1920f, 1080f);

        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 500;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return c;
    }

	private static Canvas Acquire() {
        if (TryAnchorLive(out var a)) root = a;
		else if (TryScan(out var b)) root = b;
		else {
			Plugin.Log.LogWarning("Can't find canvas, creating it");
			root = CreateOverlay();
		}
		return root;
    }

}

[HarmonyPatch(typeof(DiceKingdoms.Meta.MainMenu), nameof(DiceKingdoms.Meta.MainMenu.Start))]
public static class InterfaceAwakePatch {
    [HarmonyPostfix]
    public static void Postfix() {
		Assets.Scan();
		if (UIRoot.Root==null) {
			Plugin.Log.LogError("No canvas");
		}

		var w1 = UiKit.Window("HP test", w => {
			w.Size(360, 240)
			.Draggable()
			.Closable();

			var body = w.Body();

			int hp = 100;
			var hpLabel = body.Label($"HP: {hp}");
			body.Button("+10", () => {hp+=10; hpLabel.SetText($"HP: {hp}");});
			body.Button("-10", () => {hp-=10; hpLabel.SetText($"HP: {hp}");});
		});

		var w2 = UiKit.Window("Settings", w => {
			w.Size(360, 240).Position(80, 80).Draggable().Closable().Minimizable().Fullscreenable();

			w.Body().Label("Test minimize / fullscreen");
			w.Body().Button("Minimize", () => w.Minimize());
			w.Body().Button("Fullscreen", () => w.Fullscreen());
		});

		var w3 = UiKit.Window("About", w => {
			w.Size(280, 160).Position(200, 160).Draggable().Closable();
			w.Body().Label("Third window");
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
