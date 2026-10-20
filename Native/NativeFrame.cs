using System;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;
using GameAbout = DiceKingdoms.Meta.About;
using GameWindow = DiceKingdoms.Interface.ToggleableWindow;

namespace DiceModders.UiKit;

/// <summary>
/// The window frame of the game: the "Background" object of its windows (a sliced Background image plus an Ornament image on top). Cloning it gives a
/// window exactly the game's frame art, image settings, material and colour, instead of re-creating them from sprites.
/// Found at <c>About/Content/Tabs/Background</c>; any other window with the same Background+Ornament pair is used if About is missing.
/// </summary>
internal static class NativeFrame {
	private static Transform _template;
	private static float _next;

	private static Transform Template() {
		if (_template != null) return _template;
		float now = Time.realtimeSinceStartup;
		if (now < _next) return null;
		_next = now + 1f;
		try {
			foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<GameAbout>())) {
				var about = o?.TryCast<GameAbout>();
				if (about == null || Staging.IsOurs(about.transform)) continue;
				var frame = about.transform.Find("Content/Tabs/Background");
				if (IsFrame(frame)) return _template = frame;
			}
			foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<GameWindow>())) {
				var window = o?.TryCast<GameWindow>();
				if (window == null || Staging.IsOurs(window.transform)) continue;
				foreach (var t in window.GetComponentsInChildren<Transform>(true))
					if (t != null && t.name == "Ornament" && IsFrame(t.parent)) return _template = t.parent;
			}
		}
		catch (Exception e) { UiLog.Warn("window frame template search failed: " + e.Message); }
		return null;
	}

	private static bool IsFrame(Transform t) {
		if (t == null) return false;
		var background = t.Find("Background");
		var ornament = t.Find("Ornament");
		return background != null && ornament != null && background.GetComponent<Image>() != null && ornament.GetComponent<Image>() != null;
	}

	/// <summary>A stretched clone of the game's frame under <paramref name="parent"/>, or null if the game has none yet.</summary>
	public static RectTransform Create(Transform parent, out Image ornament) {
		ornament = null;
		var template = Template();
		if (template == null) return null;

		var go = UnityEngine.Object.Instantiate(template.gameObject, Staging.Root);   // parked: the game's scripts do not run yet
		go.name = "DM Frame";
		NativeButton.StripForeign(go);
		var layout = go.GetComponent<LayoutElement>();
		if (layout != null) UnityEngine.Object.DestroyImmediate(layout);

		var rt = go.GetComponent<RectTransform>();
		rt.SetParent(parent, false);
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
		rt.localScale = Vector3.one;
		go.SetActive(true);

		var o = go.transform.Find("Ornament");
		ornament = o != null ? o.GetComponent<Image>() : null;
		return rt;
	}
}
