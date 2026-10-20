using Il2CppInterop.Runtime;
using UnityEngine;
using ButtonIcon = DiceKingdoms.Interface.ButtonIcon;
using ButtonLabel = DiceKingdoms.Interface.ButtonLabel;
using SettingSetsInterface = DiceKingdoms.Meta.SettingSetsInterface;
using SettingInterfaceBool = DiceKingdoms.Meta.SettingInterfaceBool;
using SettingInterfaceFloat = DiceKingdoms.Meta.SettingInterfaceFloat;
using GameDropdown = DiceKingdoms.Interface.Dropdown;
using UnityEngine.UI;

namespace DiceModders.UiKit;

/// <summary>
/// Finds the live game objects we clone. Cached, but every use re-checks Unity's null test, so a template destroyed by a scene change is simply
/// found again in the new scene. Searches are throttled so a missing template never costs a scene scan per frame.
/// </summary>
internal static class NativeTemplates {
	private static GameObject _icon, _label;
	private static SettingInterfaceBool _bool;
	private static SettingInterfaceFloat _float;
	private static GameDropdown _dropdown;
	private static GameObject _close;
	private static Scrollbar _scrollbar;
	private static float _nextIcon, _nextLabel, _nextBool, _nextFloat, _nextDropdown, _nextClose, _nextScrollbar;

	/// <summary>An icon button (<c>ButtonIcon</c>): the Rules "?" button of the top-right bar.</summary>
	public static GameObject IconButton() {
		if (_icon != null) return _icon;
		float now = Time.realtimeSinceStartup;
		if (now < _nextIcon) return null;
		_nextIcon = now + 1f;
		try {
			foreach (var canvas in Object.FindObjectsOfType<Canvas>(true)) {
				if (canvas == null || !canvas.isRootCanvas) continue;
				var bar = GameUi.FindBar(canvas.transform);
				var rules = bar != null ? bar.Find(GameUi.RulesButton) : null;
				if (rules != null && rules.GetComponent<ButtonIcon>() != null) return _icon = rules.gameObject;
			}
			foreach (var b in Object.FindObjectsOfType<ButtonIcon>(true))
				if (b != null && !Staging.IsOurs(b.transform)) return _icon = b.gameObject;
		}
		catch (System.Exception e) { UiLog.Warn("icon button template search failed: " + e.Message); }
		return null;
	}

	/// <summary>A text button (<c>ButtonLabel</c>): the Close button of the Settings screen (Button-1 art, like the settings controls).</summary>
	public static GameObject LabelButton() {
		if (_label != null) return _label;
		float now = Time.realtimeSinceStartup;
		if (now < _nextLabel) return null;
		_nextLabel = now + 1f;
		try {
			foreach (var sets in Object.FindObjectsOfType<SettingSetsInterface>(true)) {
				var close = sets != null ? sets.closeButton : null;
				if (close != null && close.GetComponent<ButtonLabel>() != null) return _label = close.gameObject;
			}
			foreach (var canvas in Object.FindObjectsOfType<Canvas>(true)) {
				if (canvas == null || !canvas.isRootCanvas) continue;
				var close = canvas.transform.Find(GameUi.SettingsCloseButton);
				if (close != null && close.GetComponent<ButtonLabel>() != null) return _label = close.gameObject;
			}
			foreach (var b in Object.FindObjectsOfType<ButtonLabel>(true))
				if (b != null && !Staging.IsOurs(b.transform)) return _label = b.gameObject;
		}
		catch (System.Exception e) { UiLog.Warn("label button template search failed: " + e.Message); }
		return null;
	}

	/// <summary>The game's boolean setting control (a checkbox in the game's own style).</summary>
	public static SettingInterfaceBool BoolSetting() {
		if (_bool != null) return _bool;
		float now = Time.realtimeSinceStartup;
		if (now < _nextBool) return null;
		_nextBool = now + 1f;
		try {
			// FindObjectsOfTypeAll also returns prefab assets that the settings screens reference.
			foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<SettingInterfaceBool>())) {
				var c = o?.TryCast<SettingInterfaceBool>();
				if (c != null && !Staging.IsOurs(c.transform)) return _bool = c;
			}
		}
		catch (System.Exception e) { UiLog.Warn("bool setting template search failed: " + e.Message); }
		return null;
	}

	/// <summary>The game's float setting control (a slider with its value text).</summary>
	public static SettingInterfaceFloat FloatSetting() {
		if (_float != null) return _float;
		float now = Time.realtimeSinceStartup;
		if (now < _nextFloat) return null;
		_nextFloat = now + 1f;
		try {
			foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<SettingInterfaceFloat>())) {
				var c = o?.TryCast<SettingInterfaceFloat>();
				if (c != null && !Staging.IsOurs(c.transform) && c.GetComponentInChildren<Slider>(true) != null) return _float = c;
			}
		}
		catch (System.Exception e) { UiLog.Warn("float setting template search failed: " + e.Message); }
		return null;
	}

	/// <summary>The game's dropdown (the Language setting's): a <c>TMP_Dropdown</c> in the game's art with the game's <c>Dropdown</c> wrapper.</summary>
	public static GameDropdown Dropdown() {
		if (_dropdown != null) return _dropdown;
		float now = Time.realtimeSinceStartup;
		if (now < _nextDropdown) return null;
		_nextDropdown = now + 1f;
		try {
			foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<GameDropdown>())) {
				var c = o?.TryCast<GameDropdown>();
				if (c != null && !Staging.IsOurs(c.transform) && c.GetComponent<TMPro.TMP_Dropdown>() != null) return _dropdown = c;
			}
		}
		catch (System.Exception e) { UiLog.Warn("dropdown template search failed: " + e.Message); }
		return null;
	}

	/// <summary>A small square close button (an X) of the game's windows (About, Store ...), a <c>ButtonIcon</c> named like "Close Button".</summary>
	public static GameObject CloseIconButton() {
		if (_close != null) return _close;
		float now = Time.realtimeSinceStartup;
		if (now < _nextClose) return null;
		_nextClose = now + 1f;
		try {
			GameObject best = null;
			foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<ButtonIcon>())) {
				var b = o?.TryCast<ButtonIcon>();
				if (b == null || Staging.IsOurs(b.transform) || !b.name.Contains("Close")) continue;
				var rt = b.GetComponent<RectTransform>();
				if (rt == null) continue;
				var size = rt.rect.size;
				if (size.x < 8f || size.x > 64f || Mathf.Abs(size.x - size.y) > 2f) continue;
				if (best == null || b.gameObject.scene.IsValid()) best = b.gameObject;
				if (b.gameObject.scene.IsValid()) break;
			}
			if (best != null) return _close = best;
		}
		catch (System.Exception e) { UiLog.Warn("close button template search failed: " + e.Message); }
		return null;
	}

	/// <summary>A vertical scrollbar of the game's art (not one of the dropdown templates).</summary>
	public static Scrollbar VerticalScrollbar() {
		if (_scrollbar != null) return _scrollbar;
		float now = Time.realtimeSinceStartup;
		if (now < _nextScrollbar) return null;
		_nextScrollbar = now + 1f;
		try {
			Scrollbar best = null;
			foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<Scrollbar>())) {
				var s = o?.TryCast<Scrollbar>();
				if (s == null || Staging.IsOurs(s.transform) || s.handleRect == null) continue;
				if (s.direction != Scrollbar.Direction.BottomToTop && s.direction != Scrollbar.Direction.TopToBottom) continue;
				bool inTemplate = false;
				for (var t = s.transform.parent; t != null; t = t.parent) if (t.name == "Template") { inTemplate = true; break; }
				if (inTemplate) continue;
				if (best == null || s.gameObject.scene.IsValid()) best = s;
				if (s.gameObject.scene.IsValid()) break;
			}
			if (best != null) return _scrollbar = best;
		}
		catch (System.Exception e) { UiLog.Warn("scrollbar template search failed: " + e.Message); }
		return null;
	}
}
