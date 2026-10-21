using System;
using UnityEngine;
using UnityEngine.UI;
using GameSetInterface = DiceKingdoms.Meta.SettingSetInterface;
using GameSetsInterface = DiceKingdoms.Meta.SettingSetsInterface;

namespace DiceModders.UiKit;

/// <summary>
/// Keeps the Settings screen usable when mod rows make a tab taller than its viewport:
/// <list type="bullet">
/// <item>the game's scrollbar appears at the right edge where the Close button sits, so the button is moved left by the scrollbar's width while a tab shows it
/// (a child named "DM Shifted" remembers the shift; tabs without it are put back);</item>
/// </list>
/// </summary>
internal static class SettingsCloseShift {
	private const string Marker = "DM Shifted";

	private static GameSetInterface _set;
	private static RectTransform _setsRect;
	private static Canvas _canvas;
	private static bool _hasMods;
	private static Scrollbar _bar;
	private static float _seen;
	private static bool _described;


	/// <summary>Called by the settings hook after a tab was (re)built.</summary>
	public static void Update(GameSetInterface set, bool hasModRows) {
		_set = set;
		_hasMods = hasModRows;
		_seen = Time.unscaledTime;
		Evaluate();
	}

	/// <summary>Four times a second, from the runner: the scrollbar only gets its final size after the game's layout pass.</summary>
	public static void Tick() {
		if (_set == null || !_set.gameObject.activeInHierarchy) return;
		Evaluate();
	}

	private static void Evaluate() {
		try {
			var sets = _set.GetComponentInParent<GameSetsInterface>(true);
			var close = sets != null ? sets.closeButton : null;
			if (close == null) return;
			_setsRect = sets.GetComponent<RectTransform>();
			_canvas = sets.GetComponentInParent<Canvas>(true);

			_bar = FindBar(sets);
			if (!_described && Time.unscaledTime - _seen > 1.5f && _hasMods) { _described = true; Describe(sets); }

			SettingsViewport.Update(_set, _hasMods);
			SettingsHotspots.Ensure(_setsRect, close.transform);   // moves the unlock spot into the panel once the layout is known

			bool needs = _hasMods && _bar != null && _bar.size < 0.995f;
			float width = _bar != null ? Mathf.Max(8f, _bar.GetComponent<RectTransform>().rect.width) : 24f;

			var rt = close.GetComponent<RectTransform>();
			var marker = rt.Find(Marker);
			if (needs && marker == null) {
				rt.anchoredPosition += new Vector2(-width, 0f);
				var m = UiFactory.NewRect(Marker, rt);
				m.anchorMin = m.anchorMax = new Vector2(0.5f, 0.5f);
				m.sizeDelta = new Vector2(width, 0f);
			}
			else if (!needs && marker != null) {
				rt.anchoredPosition += new Vector2(marker.GetComponent<RectTransform>().sizeDelta.x, 0f);
				UnityEngine.Object.DestroyImmediate(marker.gameObject);
			}
		}
		catch (Exception e) { UiLog.Once("closeshift", "close button shift: " + e.Message); }
	}

	/// <summary>The settings screen's vertical scrollbar (dropdown scrollbars are in inactive templates and are skipped).</summary>
	private static Scrollbar FindBar(GameSetsInterface sets) {
		foreach (var b in sets.GetComponentsInChildren<Scrollbar>(false)) {
			if (b == null) continue;
			var r = b.GetComponent<RectTransform>();
			if (r.rect.height > 100f && r.rect.height > r.rect.width * 2f) return b;
		}
		return null;
	}

	// ------------------------------------------------------------------ diagnostics

	private static void Describe(GameSetsInterface sets) {
		var sb = new System.Text.StringBuilder("settings scroll structure:");
		foreach (var b in sets.GetComponentsInChildren<Scrollbar>(true)) {
			if (b == null) continue;
			sb.Append($"\n   scrollbar '{b.name}' active={b.gameObject.activeInHierarchy} size={b.size:0.###} value={b.value:0.###} direction={b.direction} rect={b.GetComponent<RectTransform>().rect}");
		}
		foreach (var s in sets.GetComponentsInChildren<ScrollRect>(true)) {
			if (s == null) continue;
			sb.Append($"\n   scrollrect '{s.name}' enabled={s.enabled} vertical={s.vertical} sensitivity={s.scrollSensitivity} movement={s.movementType} content='{(s.content != null ? s.content.name : "-")}' bar='{(s.verticalScrollbar != null ? s.verticalScrollbar.name : "-")}'");
		}
		UiLog.Info(sb.ToString());
	}
}
