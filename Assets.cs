using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Il2CppInterop.Runtime;

namespace DiceModders.UiKit;

/// <summary>
/// The game's own sprites, fonts and materials, looked up by name. Windows, ornaments and icons use these; buttons, checkboxes and tooltips do not
/// need them any more because they are cloned from the game's live objects (see <c>Native/</c>).
/// The cache is validated with Unity's null test on every lookup: assets destroyed by a scene change are reloaded instead of drawing as white boxes.
/// </summary>
public static class Assets {
	private static readonly Dictionary<string, Sprite> sprites = new();
	private static readonly Dictionary<string, Material> materials = new();
	private static readonly Dictionary<string, TMP_FontAsset> fonts = new();
	private static bool scanned;
	private static float nextRescan;

	/// <summary>Reads every loaded sprite, material and font asset once. Called automatically on first use; safe to call again.</summary>
	public static void Scan() {
		sprites.Clear();
		materials.Clear();
		fonts.Clear();

		foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.From(typeof(Sprite))))
			if (o?.TryCast<Sprite>() is { } s && !string.IsNullOrEmpty(s.name))
				sprites.TryAdd(s.name, s);

		foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.From(typeof(Material))))
			if (o?.TryCast<Material>() is { } m && !string.IsNullOrEmpty(m.name))
				materials.TryAdd(m.name, m);

		foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.From(typeof(TMP_FontAsset))))
			if (o?.TryCast<TMP_FontAsset>() is { } f && !string.IsNullOrEmpty(f.name))
				fonts.TryAdd(f.name, f);

		scanned = true;
		nextRescan = Time.realtimeSinceStartup + 3f;
	}

	private static void EnsureScanned() { if (!scanned) Scan(); }

	/// <summary>Looks a name up; when the entry is missing or destroyed the assets are re-read (at most every 3 seconds).</summary>
	private static T Lookup<T>(Dictionary<string, T> map, string name) where T : UnityEngine.Object {
		if (name == null) return null;
		EnsureScanned();
		if (map.TryGetValue(name, out var found) && found != null) return found;
		if (Time.realtimeSinceStartup >= nextRescan) {
			Scan();
			if (map.TryGetValue(name, out found) && found != null) return found;
		}
		return null;
	}

	public static Sprite GetSprite(string name) => Lookup(sprites, name);
	public static Material GetMaterial(string name) => Lookup(materials, name);
	public static TMP_FontAsset GetFont(string name) => Lookup(fonts, name);

	/// <summary>Names of the game's sprites that contain <paramref name="contains"/> (all of them when null). Handy to find icon names.</summary>
	public static IEnumerable<string> SpriteNames(string contains = null) {
		EnsureScanned();
		foreach (var kv in sprites)
			if (kv.Value != null && (contains == null || kv.Key.IndexOf(contains, System.StringComparison.OrdinalIgnoreCase) >= 0))
				yield return kv.Key;
	}

	/// <summary>Creates a sprite from PNG bytes (for a mod's own icons). Returns null when the image cannot be decoded.</summary>
	public static Sprite SpriteFromPng(byte[] png, string name = "DM custom icon", float pixelsPerUnit = 100f) {
		try {
			var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			if (!ImageConversion.LoadImage(texture, png)) return null;
			texture.name = name;
			texture.filterMode = FilterMode.Bilinear;
			var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
			sprite.name = name;
			return sprite;
		}
		catch (System.Exception e) {
			UiLog.Warn("SpriteFromPng failed: " + e.Message);
			return null;
		}
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

	/// <remarks>
	/// <c>Classic</c> was added later (Ornament-3, the frame most of the game's windows use); the first four values are unchanged.
	/// </remarks>
	public enum OrnamentStyle { None = 0, Border, BorderWithTwoCorners, BorderWithFourCorners, Classic }

	public static Sprite Ornament(OrnamentStyle style) => style switch {
		OrnamentStyle.Border                => GetSprite("Ornament-1"),
		OrnamentStyle.BorderWithTwoCorners  => GetSprite("Ornament-2"),
		OrnamentStyle.BorderWithFourCorners => GetSprite("Ornament-5"),
		OrnamentStyle.Classic               => GetSprite("Ornament-3"),
		_                                   => null,
	};

	public static TMP_FontAsset Font => GetFont("Main-Font-Normal") ?? FontFromLive();
	public static Material FontShadowMaterial => GetMaterial("Main-Font-Normal-Shadow");
}
