using System;
using UnityEngine;
using UnityEngine.UI;
using GameSetInterface = DiceKingdoms.Meta.SettingSetInterface;
using GameSetsInterface = DiceKingdoms.Meta.SettingSetsInterface;

namespace DiceModders.UiKit;

/// <summary>
/// When mod rows make a tab taller than its viewport the game's scrollbar appears at the right edge, where the Close button sits. The button is moved left
/// by the scrollbar's width while such a tab is shown and put back on tabs without it. A child named "DM Shifted" remembers the shift.
/// </summary>
internal static class SettingsCloseShift {
	private const string Marker = "DM Shifted";

	/// <param name="hasModRows">True when mods added rows to this tab (tabs without mod rows are left as the game made them, only un-shifted).</param>
	public static void Update(GameSetInterface set, bool hasModRows) {
		try {
			if (set == null || !set.gameObject.activeInHierarchy) return;
			var sets = set.GetComponentInParent<GameSetsInterface>(true);
			var close = sets != null ? sets.closeButton : null;
			if (close == null) return;

			bool needs = false;
			float width = 24f;
			var scroll = set.GetComponentInParent<ScrollRect>(true);
			if (hasModRows && scroll != null && scroll.content != null) {
				Canvas.ForceUpdateCanvases();
				LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
				var view = scroll.viewport != null ? scroll.viewport : scroll.GetComponent<RectTransform>();
				needs = scroll.vertical && scroll.content.rect.height > view.rect.height + 1f;
				var bar = scroll.verticalScrollbar;
				if (bar != null) width = Mathf.Max(8f, bar.GetComponent<RectTransform>().rect.width);
			}

			var rt = close.GetComponent<RectTransform>();
			var marker = rt.Find(Marker);
			if (needs && marker == null) {
				rt.anchoredPosition += new Vector2(-width, 0f);
				var m = new GameObject(Marker, new[] { Il2CppInterop.Runtime.Il2CppType.Of<RectTransform>() });
				m.transform.SetParent(rt, false);
				m.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 0f);
			}
			else if (!needs && marker != null) {
				rt.anchoredPosition += new Vector2(marker.GetComponent<RectTransform>().sizeDelta.x, 0f);
				UnityEngine.Object.DestroyImmediate(marker.gameObject);
			}
		}
		catch (Exception e) { UiLog.Once("closeshift", "close button shift: " + e.Message); }
	}
}
