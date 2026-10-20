using System;
using HarmonyLib;
using UnityEngine;
using GameSetInterface = DiceKingdoms.Meta.SettingSetInterface;
using GameSetsInterface = DiceKingdoms.Meta.SettingSetsInterface;

namespace DiceModders.UiKit;

/// <summary>
/// Postfix on the game's own <c>SettingSetInterface.GenerateElements</c> (it clears and rebuilds every row of a tab). After the game has built its rows,
/// the registered blocks are appended with the game's own row prefabs. No scanning, no timers: it runs exactly when the game rebuilds the tab.
/// </summary>
[HarmonyPatch(typeof(GameSetInterface), "GenerateElements")]
internal static class SettingsHook {
	private static bool _installed;

	public static void Install() {
		if (_installed) return;
		_installed = true;
		try { new Harmony("DiceModders.UiKit.settings").PatchAll(typeof(SettingsHook)); }
		catch (Exception e) { UiLog.Error("settings hook failed: " + e); }
	}

	[HarmonyPostfix]
	public static void Postfix(GameSetInterface __instance) {
		try {
			if (__instance == null || __instance.holder == null) return;
			if (!TryGetTab(__instance, out var tab)) return;
			var regs = SettingsRegistry.For(tab);
			if (regs.Count == 0) return;

			RemoveOld(__instance);
			int n = 0;
			foreach (var reg in regs) {
				var section = new SettingsSection(__instance, (++n).ToString());
				try {
					section.Header(reg.Title);
					reg.Build(section);
				}
				catch (Exception e) { UiLog.Error($"settings block '{reg.Title}' failed: {e}"); }
			}
			UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(__instance.holder);
		}
		catch (Exception e) { UiLog.Once("settings:hook", "settings hook: " + e); }
	}

	private static void RemoveOld(GameSetInterface set) {
		var holder = set.holder;
		for (int i = holder.childCount - 1; i >= 0; i--) {
			var c = holder.GetChild(i);
			if (c.name.StartsWith("DM Settings")) { c.gameObject.SetActive(false); UnityEngine.Object.Destroy(c.gameObject); }
		}
	}

	private static bool TryGetTab(GameSetInterface set, out SettingsTab tab) {
		tab = SettingsTab.Video;
		var sets = set.GetComponentInParent<GameSetsInterface>(true);
		if (sets == null) return false;
		IntPtr p = set.Pointer;
		if (sets.video != null && sets.video.Pointer == p) { tab = SettingsTab.Video; return true; }
		if (sets.audio != null && sets.audio.Pointer == p) { tab = SettingsTab.Audio; return true; }
		if (sets.input != null && sets.input.Pointer == p) { tab = SettingsTab.Input; return true; }
		if (sets.game  != null && sets.game.Pointer  == p) { tab = SettingsTab.Game;  return true; }
		return false;
	}
}
