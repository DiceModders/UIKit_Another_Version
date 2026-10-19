using Il2CppSystem.Collections.Generic;
using UnityEngine;
using TMPro;
using Il2CppInterop.Runtime;

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

	public enum OrnamentStyle { None = 0, Border, BorderWithTwoCorners, BorderWithFourCorners }
	public static Sprite Ornament(OrnamentStyle style) => style switch {
		OrnamentStyle.Border                => GetSprite("Ornament-1"),
		OrnamentStyle.BorderWithTwoCorners  => GetSprite("Ornament-2"),
		OrnamentStyle.BorderWithFourCorners => GetSprite("Ornament-5"),
		_                                   => null,
	};

    public static TMP_FontAsset Font => GetFont("Main-Font-Normal") ?? FontFromLive();
    public static Material FontShadowMaterial => GetMaterial("Main-Font-Normal-Shadow");
}
