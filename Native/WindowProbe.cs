using System;
using System.Text;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameWindow = DiceKingdoms.Interface.ToggleableWindow;

namespace DiceModders.UiKit;

/// <summary>
/// Debug helper: writes the object tree of the game's own windows (About, Rules, Store, Settings, Technology...) to the log, so a window frame can be
/// cloned from one of them. Enabled with the config entry [Debug] DumpWindows. Does nothing otherwise.
/// </summary>
internal static class WindowProbe {
	private const int MaxDepth = 6;
	private const int MaxLines = 220;

	public static void Dump() {
		try {
			var seen = new System.Collections.Generic.HashSet<string>();
			foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<GameWindow>())) {
				var w = o?.TryCast<GameWindow>();
				if (w == null || Staging.IsOurs(w.transform)) continue;
				string kind = w.GetIl2CppType().Name;
				if (!seen.Add(kind + "/" + w.name)) continue;
				var sb = new StringBuilder();
				sb.Append($"window '{w.name}' ({kind}) active={w.gameObject.activeInHierarchy}");
				int lines = 0;
				Walk(w.transform, 0, sb, ref lines);
				UiLog.Info(sb.ToString());
			}
		}
		catch (Exception e) { UiLog.Warn("window probe failed: " + e.Message); }
	}

	private static void Walk(Transform t, int depth, StringBuilder sb, ref int lines) {
		if (depth > MaxDepth || lines > MaxLines) return;
		var rt = t.GetComponent<RectTransform>();
		sb.Append('\n').Append(' ', 3 + depth * 2).Append(t.name);
		if (rt != null) sb.Append($" [{rt.rect.width:0.#}x{rt.rect.height:0.#} anchors {rt.anchorMin}-{rt.anchorMax} pos {rt.anchoredPosition}]");
		if (!t.gameObject.activeSelf) sb.Append(" (inactive)");
		var parts = new System.Collections.Generic.List<string>();
		foreach (var c in t.GetComponents<Component>()) {
			if (c == null) continue;
			string n = c.GetIl2CppType().Name;
			if (n == "RectTransform" || n == "CanvasRenderer") continue;
			if (c is Image img) n += $"(sprite={(img.sprite != null ? img.sprite.name : "-")},type={img.type})";
			else if (c is TMP_Text tmp) n += $"(\"{Trim(tmp.text)}\")";
			parts.Add(n);
		}
		if (parts.Count > 0) sb.Append(" : ").Append(string.Join(", ", parts));
		lines++;
		for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), depth + 1, sb, ref lines);
	}

	private static string Trim(string s) => s == null ? "" : (s.Length > 24 ? s.Substring(0, 24) : s).Replace("\n", " ");
}
