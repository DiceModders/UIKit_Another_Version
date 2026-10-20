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
		try {
			var harmony = new Harmony("DiceModders.UiKit.settings");
			harmony.PatchAll(typeof(SettingsHook));
			harmony.PatchAll(typeof(SettingsOpenedHook));
		}
		catch (Exception e) { UiLog.Error("settings hook failed: " + e); }
	}

	[HarmonyPostfix]
	public static void Postfix(GameSetInterface __instance) {
		try {
			if (__instance == null || __instance.holder == null) return;
			if (!TryGetTab(__instance, out var tab)) return;
			var regs = SettingsRegistry.For(tab);
			if (regs.Count == 0) { if (tab != SettingsTab.Lobby) SettingsCloseShift.Update(__instance, false); return; }

			RemoveOld(__instance);
			SettingsBuilder.MeasureContentLeft(__instance);
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
			if (tab != SettingsTab.Lobby) SettingsCloseShift.Update(__instance, true);
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

	/// <summary>The match-start screen (<c>StartGame</c>) owns the lobby settings box as its <c>lobbySettingsInterface</c>.</summary>
	private static bool IsLobbySet(GameSetInterface set) {
		var start = set.GetComponentInParent<DiceKingdoms.Interface.StartGame>(true);
		if (start != null) return start.lobbySettingsInterface != null && start.lobbySettingsInterface.Pointer == set.Pointer;
		foreach (var candidate in UnityEngine.Object.FindObjectsOfType<DiceKingdoms.Interface.StartGame>(true))
			if (candidate != null && candidate.lobbySettingsInterface != null && candidate.lobbySettingsInterface.Pointer == set.Pointer) return true;
		return false;
	}

	private static bool TryGetTab(GameSetInterface set, out SettingsTab tab) {
		tab = SettingsTab.Video;
		if (IsLobbySet(set)) { tab = SettingsTab.Lobby; return true; }
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

/// <summary>Postfix on <c>SettingSetsInterface.OnEnable</c>: the Settings screen was shown.</summary>
[HarmonyPatch(typeof(GameSetsInterface), "OnEnable")]
internal static class SettingsOpenedHook {
	[HarmonyPostfix]
	public static void Postfix(GameSetsInterface __instance) {
		try {
			if (__instance == null) return;
			var rect = __instance.GetComponent<UnityEngine.RectTransform>();
			if (rect == null) return;
			SettingsHotspots.Ensure(rect);
			UiKit.Settings.RaiseOpened(rect);
		}
		catch (Exception e) { UiLog.Once("settings:opened", "settings opened hook: " + e); }
	}
}
