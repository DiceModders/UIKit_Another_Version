using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using GameRules = DiceKingdoms.Meta.Rules;
using GameDropdown = DiceKingdoms.Interface.Dropdown;
using GameCivilization = DiceKingdoms.Interface.Civilization;
using GameTechnologies = DiceKingdoms.Game.Technologies;
using GameTechTree = DiceKingdoms.Game.TechTree;
using GamePlayerSkins = DiceKingdoms.Game.PlayerSkins;

namespace DiceModders.UiKit;

/// <summary>
/// Hooks into the game's own Rules screen. The civilization drop-down is a <c>Dropdown</c> filled from the game's <c>Civilization</c> enum when the screen wakes up;
/// after that the registered civilizations are appended to its entries (a TMP option plus the matching boxed enum value the game's select handler expects).
/// </summary>
internal static class RulesHook {
	private static bool _installed;
	private static readonly HashSet<long> Added = new();   // (dropdown instance, civilization id) pairs already appended

	public static void Install() {
		if (_installed) return;
		_installed = true;
		try {
			var harmony = new Harmony("DiceModders.UiKit.rules");
			harmony.PatchAll(typeof(RulesAwakeHook));
			harmony.PatchAll(typeof(RulesEnableHook));
			harmony.PatchAll(typeof(TechTreeLookupHook));
			harmony.PatchAll(typeof(PlayerSkinsCivilizationHook));
			UiLog.Info("Rules hooks installed");
		}
		catch (Exception e) { UiLog.Error("rules hook failed: " + e); }
	}

	internal static void Apply(GameRules rules) {
		var dropdown = rules != null ? rules.civilizationDropdown : null;
		UiLog.Info($"Rules: screen hook ran (dropdown {(dropdown != null ? "found" : "MISSING")}, {RulesRegistry.All.Count} civilization(s) registered)");
		if (dropdown == null) return;
		foreach (var civ in RulesRegistry.All) {
			long key = ((long)dropdown.GetInstanceID() << 32) ^ (uint)civ.Id;
			if (Added.Contains(key)) continue;
			try {
				Append(dropdown, civ);
				Added.Add(key);
			}
			catch (Exception e) { UiLog.Error($"adding civilization '{civ.Name}' to the Rules screen failed: {e}"); }
		}
	}

	private static void Append(GameDropdown dropdown, RulesCivilization civ) {
		var tmp = dropdown.dropdown;
		if (tmp == null) throw new InvalidOperationException("the Rules dropdown has no TMP_Dropdown");

		var old = dropdown.values;
		int n = old != null ? old.Length : 0;
		var values = new Il2CppReferenceArray<Il2CppSystem.Object>(n + 1);
		for (int i = 0; i < n; i++) values[i] = old[i];
		values[n] = Il2CppSystem.Enum.ToObject(Il2CppType.Of<GameCivilization>(), civ.Id);   // a boxed Civilization: the game's handler unboxes it as such
		dropdown.values = values;

		tmp.options.Add(new TMP_Dropdown.OptionData(civ.Name));
		tmp.RefreshShownValue();
		UiLog.Info($"Rules: civilization '{civ.Name}' ({civ.Id}) added to the drop-down");
	}
}

[HarmonyPatch(typeof(GameRules), "Awake")]
internal static class RulesAwakeHook {
	[HarmonyPostfix]
	public static void Postfix(GameRules __instance) {
		try { RulesHook.Apply(__instance); }
		catch (Exception e) { UiLog.Once("rules:awake", "rules awake hook: " + e); }
	}
}

[HarmonyPatch(typeof(GameRules), "OnEnable")]
internal static class RulesEnableHook {
	[HarmonyPostfix]
	public static void Postfix(GameRules __instance) {
		try {
			RulesHook.Apply(__instance);   // civilizations registered after the screen first woke up
			var rect = __instance != null ? __instance.GetComponent<RectTransform>() : null;
			if (rect != null) UiKit.Rules.RaiseOpened(rect);
		}
		catch (Exception e) { UiLog.Once("rules:enable", "rules enable hook: " + e); }
	}
}

/// <summary>
/// <c>Technologies.TechTree(civilization)</c> picks the tree out of the game's list with <c>First(...)</c>, which throws for a civilization the game does not know.
/// For civilizations registered with a tree provider the provider's tree is returned instead.
/// </summary>
[HarmonyPatch(typeof(GameTechnologies), "TechTree")]
internal static class TechTreeLookupHook {
	[HarmonyPrefix]
	public static bool Prefix(GameCivilization civilization, ref GameTechTree __result) {
		try {
			if (RulesRegistry.TryGetTree((int)civilization, out var tree)) { __result = tree; return false; }
		}
		catch (Exception e) { UiLog.Once("rules:tree", "tech tree hook: " + e); }
		return true;
	}
}

/// <summary>
/// <c>PlayerSkins.Civilization</c>'s setter derives the biome with a switch over the five built-in civilizations and throws <c>ArgumentOutOfRangeException</c> for any other value.
/// For a registered civilization the built-in one it is "like" is set first (biome, visual caches), then the civilization number itself is stored.
/// </summary>
[HarmonyPatch(typeof(GamePlayerSkins), "Civilization", MethodType.Setter)]
internal static class PlayerSkinsCivilizationHook {
	private static bool _busy;

	[HarmonyPrefix]
	public static bool Prefix(GamePlayerSkins __instance, GameCivilization value) {
		if (_busy) return true;
		try {
			int id = (int)value;
			if (id >= 0 && id <= 4) return true;
			if (!RulesRegistry.TryGetSkinsLike(id, out int like)) return true;
			_busy = true;
			try {
				__instance.Civilization = (GameCivilization)like;   // runs the game's own setter for the borrowed civilization
				__instance.civilization = value;                    // and remembers the real one
			}
			finally { _busy = false; }
			return false;
		}
		catch (Exception e) { UiLog.Once("rules:skins", "civilization setter hook: " + e); }
		return true;
	}
}
