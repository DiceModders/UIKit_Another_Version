using Il2CppInterop.Runtime;
using UnityEngine;
using ButtonIcon = DiceKingdoms.Interface.ButtonIcon;
using ButtonLabel = DiceKingdoms.Interface.ButtonLabel;
using SettingSetsInterface = DiceKingdoms.Meta.SettingSetsInterface;
using SettingInterfaceBool = DiceKingdoms.Meta.SettingInterfaceBool;

namespace DiceModders.UiKit;

/// <summary>
/// Finds the live game objects we clone. Cached, but every use re-checks Unity's null test, so a template destroyed by a scene change is simply
/// found again in the new scene. Searches are throttled so a missing template never costs a scene scan per frame.
/// </summary>
internal static class NativeTemplates {
	private static GameObject _icon, _label;
	private static SettingInterfaceBool _bool;
	private static float _nextIcon, _nextLabel, _nextBool;

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
}
