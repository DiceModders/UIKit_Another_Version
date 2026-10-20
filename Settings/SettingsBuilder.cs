using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameSetInterface = DiceKingdoms.Meta.SettingSetInterface;
using GameEntry = DiceKingdoms.Meta.SettingEntryInterface;

namespace DiceModders.UiKit;

/// <summary>Creates single settings rows from the game's own row prefabs (entry = label + content holder, header).</summary>
internal static class SettingsBuilder {
	private const float StepButtonWidth = 32f;
	private const float ValueWidth = 90f;

	public static void AddHeader(GameSetInterface set, string name, string text) {
		var prefab = set.headerPrefab;
		if (prefab == null) { UiLog.Once("settings:header", "settings: headerPrefab missing"); return; }
		var header = UnityEngine.Object.Instantiate(prefab, set.holder);
		header.gameObject.name = name;
		SetLabel(header.visual, text);
	}

	public static void AddInfo(GameSetInterface set, string name, string text) {
		var prefab = set.headerPrefab;
		if (prefab == null) return;
		var info = UnityEngine.Object.Instantiate(prefab, set.holder);
		info.gameObject.name = name;
		SetLabel(info.visual, text);
		if (info.visual != null) {
			info.visual.enableWordWrapping = true;
			info.visual.alignment = TextAlignmentOptions.TopLeft;
		}
		var le = info.GetComponent<LayoutElement>();
		if (le != null) { le.preferredWidth = -1; le.minWidth = -1; le.flexibleWidth = 1; }
	}

	public static void AddToggle(GameSetInterface set, string name, string label, Func<bool> get, Action<bool> setValue) {
		var entry = NewEntry(set, name, label);
		if (entry == null) return;
		var holder = entry.contentHolder;
		var toggle = NativeToggle.Create(NativeTemplates.BoolSetting(), holder, SafeGet(get), v => {
			try { setValue?.Invoke(v); }
			catch (Exception e) { UiLog.Error($"settings toggle '{label}': {e}"); }
		});
		if (toggle == null) { UiLog.Once("settings:toggle", "settings: no native checkbox template found"); return; }
		SettingsLive.Add(entry, () => toggle.SetSilently(SafeGet(get)));
		Rebuild(set);
	}

	public static void AddButtons(GameSetInterface set, string name, string label, (string text, Action onClick)[] buttons) {
		var entry = NewEntry(set, name, label);
		if (entry == null) return;
		var holder = entry.contentHolder;
		EnsureRow(holder);
		foreach (var (text, onClick) in buttons) {
			var click = onClick;
			var b = NativeButton.CreateLabel(holder, name + " " + text, text, 16f, () => {
				try { click?.Invoke(); }
				catch (Exception e) { UiLog.Error($"settings button '{text}': {e}"); }
			});
			if (b == null) { UiLog.Once("settings:button", "settings: no native text button template found"); continue; }
			b.SetSize(Mathf.Max(80f, 14f * (text?.Length ?? 0) + 24f), NativeButton.Height);
		}
		Rebuild(set);
	}

	public static void AddStepper(GameSetInterface set, string name, string label, Func<string> valueText, Action<int> step) {
		var entry = NewEntry(set, name, label);
		if (entry == null) return;
		var holder = entry.contentHolder;
		EnsureRow(holder);

		NativeButton.CreateLabel(holder, name + " Minus", "-", 20f, () => Step(step, -1))?.SetSize(StepButtonWidth, NativeButton.Height);

		var value = NativeText.Create(holder, name + " Value", SafeText(valueText), 16f, TextAlignmentOptions.Center);
		var le = value.gameObject.AddComponent<LayoutElement>();
		le.preferredWidth = ValueWidth; le.minWidth = ValueWidth; le.preferredHeight = NativeButton.Height;
		SettingsLive.Add(entry, () => value.text = SafeText(valueText));

		NativeButton.CreateLabel(holder, name + " Plus", "+", 20f, () => { Step(step, +1); value.text = SafeText(valueText); })?.SetSize(StepButtonWidth, NativeButton.Height);
		Rebuild(set);
	}

	private static void Step(Action<int> step, int delta) {
		try { step?.Invoke(delta); }
		catch (Exception e) { UiLog.Error("settings stepper: " + e); }
	}

	private static GameEntry NewEntry(GameSetInterface set, string name, string label) {
		var prefab = set.settingEntryInterfacePrefab;
		if (prefab == null) { UiLog.Once("settings:entry", "settings: settingEntryInterfacePrefab missing"); return null; }
		var entry = UnityEngine.Object.Instantiate(prefab, set.holder);
		entry.gameObject.name = name;
		SetLabel(entry.label?.visual, label);
		return entry;
	}

	private static void SetLabel(TMP_Text visual, string text) {
		if (visual != null) visual.text = text ?? "";
	}

	/// <summary>The game's content holder may already lay its children out; add a right aligned row only when it does not.</summary>
	private static void EnsureRow(RectTransform holder) {
		if (holder == null || holder.GetComponent<HorizontalOrVerticalLayoutGroup>() != null) return;
		var h = holder.gameObject.AddComponent<HorizontalLayoutGroup>();
		h.childAlignment = TextAnchor.MiddleRight;
		h.childControlWidth = false; h.childControlHeight = false;
		h.childForceExpandWidth = false; h.childForceExpandHeight = false;
		h.spacing = 6f;
	}

	private static void Rebuild(GameSetInterface set) {
		if (set.holder != null) LayoutRebuilder.MarkLayoutForRebuild(set.holder);
	}

	private static bool SafeGet(Func<bool> get) { try { return get != null && get(); } catch (Exception e) { UiLog.Once("settings:get", "settings getter: " + e.Message); return false; } }
	private static string SafeText(Func<string> f) { try { return f?.Invoke() ?? ""; } catch (Exception e) { UiLog.Once("settings:text", "settings value text: " + e.Message); return ""; } }
}
