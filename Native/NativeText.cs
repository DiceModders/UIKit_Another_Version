using TMPro;
using UnityEngine;

namespace DiceModders.UiKit;

/// <summary>
/// Text that looks like the game's own. A TMP font asset must be used with the material it was made for (a mismatched pair draws garbage glyphs),
/// so font + material are always copied together from a live game text instead of being looked up separately by name.
/// </summary>
internal static class NativeText {
	public static readonly Color DefaultColor = new Color(0.969f, 0.940f, 0.893f, 1f);

	private static TMP_FontAsset _font;
	private static Material _material;
	private static float _nextScan;

	/// <summary>Remembers the font/material pair of a game text we already hold (no scene scan needed).</summary>
	public static void LearnFrom(TMP_Text donor) {
		if (donor == null || donor.font == null) return;
		_font = donor.font;
		_material = donor.fontSharedMaterial;
	}

	private static bool EnsurePair() {
		if (_font != null && _material != null) return true;
		float now = Time.realtimeSinceStartup;
		if (now >= _nextScan) {
			_nextScan = now + 3f;
			try {
				foreach (var t in Object.FindObjectsOfType<TMP_Text>()) {
					if (t == null || t.font == null || !t.gameObject.activeInHierarchy) continue;
					if (Staging.IsOurs(t.transform)) continue;
					var m = t.fontSharedMaterial;
					if (t.font.name == "Main-Font-Normal" && m != null && m.name.Contains("Shadow")) {
						_font = t.font; _material = m;
						break;
					}
				}
			}
			catch (System.Exception e) { UiLog.Warn("font donor scan failed: " + e.Message); }
		}
		if (_font == null) { _font = Assets.Font; _material = Assets.FontShadowMaterial; }
		return _font != null;
	}

	/// <summary>Applies the game's font + material pair to a text.</summary>
	public static void Apply(TMP_Text text) {
		if (text == null || !EnsurePair()) return;
		text.font = _font;
		if (_material != null) text.fontSharedMaterial = _material;
	}

	public static TextMeshProUGUI Create(RectTransform parent, string name, string value, float size, TextAlignmentOptions align, Color? color = null) {
		var rt = UiFactory.NewRect(name, parent);
		var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
		Apply(tmp);
		tmp.text = value ?? string.Empty;
		tmp.fontSize = size;
		tmp.alignment = align;
		tmp.color = color ?? DefaultColor;
		tmp.raycastTarget = false;
		return tmp;
	}

	/// <summary>Makes a text shrink instead of spilling out of its rectangle.</summary>
	public static void FitInRect(TMP_Text text, float maxSize, float minSize = 8f) {
		if (text == null) return;
		text.enableWordWrapping = false;
		text.enableAutoSizing = true;
		text.fontSizeMin = minSize;
		text.fontSizeMax = maxSize;
	}
}
