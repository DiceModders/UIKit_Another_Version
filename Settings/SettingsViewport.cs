using System;
using UnityEngine;
using UnityEngine.UI;
using GameSetInterface = DiceKingdoms.Meta.SettingSetInterface;
using GameSetsInterface = DiceKingdoms.Meta.SettingSetsInterface;

namespace DiceModders.UiKit;

/// <summary>
/// The game's Close button floats over the bottom right of the Settings scroll area, so rows scrolling past it show up behind it and run to the window
/// edge. On tabs with mod rows, the scroll area's own clip mask (<see cref="RectMask2D"/>) is pulled in at the bottom up to the top of the Close button:
/// rows are simply cut there. No rectangle moves and no layout changes, so the scrollbar and everything else behave exactly as before; the bottom spacer
/// lets the last row scroll up clear of the line.
/// </summary>
internal static class SettingsViewport {
	private const float Gap = 8f;
	private static int _logs;

	/// <summary>
	/// Mouse wheel speed of the game's own ScrollRect (its <c>scrollSensitivity</c>) on tabs with mod rows. The game's value (3) suits its short tabs; with
	/// mod rows the list is several times longer, so each wheel notch moves the list further.
	/// </summary>
	public const float WheelSensitivity = 12f;

	public static void SetWheelSpeed(GameSetInterface set) {
		var sets = set.GetComponentInParent<GameSetsInterface>(true);
		var scroll = FindScroll(sets, set);
		if (scroll != null && scroll.scrollSensitivity < WheelSensitivity) scroll.scrollSensitivity = WheelSensitivity;
	}

	/// <summary>The scroll view whose content is this tab's holder (falls back to the nearest ScrollRect above it).</summary>
	private static ScrollRect FindScroll(GameSetsInterface sets, GameSetInterface set) {
		if (sets != null && set.holder != null) {
			foreach (var s in sets.GetComponentsInChildren<ScrollRect>(true))
				if (s != null && s.content != null && s.content.Pointer == set.holder.Pointer) return s;
		}
		return set.holder != null ? set.holder.GetComponentInParent<ScrollRect>(true) : null;
	}

	public static void Update(GameSetInterface set, bool hasMods) {
		var sets = set.GetComponentInParent<GameSetsInterface>(true);
		var close = sets != null ? sets.closeButton : null;
		var scroll = FindScroll(sets, set);
		if (scroll == null || close == null) { UiLog.Once("viewport:missing", $"settings: scroll area or Close button not found (scroll={(scroll != null)}, close={(close != null)})"); return; }

		var mask = scroll.GetComponentInChildren<RectMask2D>(true);
		if (mask == null) { UiLog.Once("viewport:nomask", "settings: no RectMask2D in the scroll area, rows cannot be cut above the Close button"); return; }
		var rt = mask.GetComponent<RectTransform>();
		if (rt.rect.width < 1f || rt.rect.height < 1f) return;   // not laid out yet (the screen is still opening); the next tick tries again

		float want = 0f;
		if (hasMods) {
			// world space, so pivots, anchors and the screen's scale do not matter; converted to the mask's own units at the end
			var closeCorners = new Vector3[4];
			var maskCorners = new Vector3[4];
			close.GetComponent<RectTransform>().GetWorldCorners(closeCorners);
			rt.GetWorldCorners(maskCorners);
			float scale = Mathf.Abs(rt.lossyScale.y);
			if (scale > 0.00001f) {
				float closeTop = closeCorners[1].y, maskBottom = maskCorners[0].y, maskTop = maskCorners[1].y;
				float needed = (closeTop - maskBottom) / scale + Gap;
				UiLog.Once("viewport:calc", $"settings viewport numbers: closeTop={closeTop:0.#} maskBottom={maskBottom:0.#} maskTop={maskTop:0.#} scale={scale:0.####} needed={needed:0.#}");
				if (needed > Gap && closeTop < maskTop) want = Mathf.Min(needed, rt.rect.height * 0.5f);
			}
		}

		var padding = mask.padding;   // (left, bottom, right, top)
		if (Mathf.Abs(padding.y - want) < 0.5f) return;
		padding.y = want;
		mask.padding = padding;
		if (_logs++ < 8) UiLog.Info($"settings viewport: mask '{mask.name}' rect={rt.rect} -> rows cut {want:0.#} units above the bottom, clear of the Close button");
	}
}
