using System;
using System.Collections.Generic;

namespace DiceModders.UiKit;

/// <summary>Keeps rows in sync with the mod's own state while the settings screen is open (4 times a second, only for live rows).</summary>
internal static class SettingsLive {
	private static readonly List<(UnityEngine.Object Owner, Action Refresh)> Rows = new();
	private static float _next;

	public static void Add(UnityEngine.Object owner, Action refresh) { if (owner != null && refresh != null) Rows.Add((owner, refresh)); }

	public static void Tick(float now) {
		if (Rows.Count == 0 || now < _next) return;
		_next = now + 0.25f;
		Rows.RemoveAll(r => r.Owner == null);
		foreach (var r in Rows.ToArray()) {
			try { r.Refresh(); }
			catch (Exception e) { UiLog.Once("settingslive", "settings refresh: " + e.Message); }
		}
	}
}
