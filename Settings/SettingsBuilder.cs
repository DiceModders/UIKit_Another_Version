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
	private const float ValueWidth = 64f;

	/// <summary>Distance from the left edge of a content column to where the game's own controls start (measured from a native row; the game's controls are inset).</summary>
	public static float ContentLeft;

	public static void MeasureContentLeft(GameSetInterface set) {
		ContentLeft = 0f;
		try {
			var holder = set.holder;
			LayoutRebuilder.ForceRebuildLayoutImmediate(holder);
			for (int i = 0; i < holder.childCount; i++) {
				var row = holder.GetChild(i);
				if (row == null || row.name.StartsWith("DM")) continue;
				var entry = row.GetComponent<GameEntry>();
				var column = entry != null ? entry.contentHolder : null;
				if (column == null) continue;

				// the leftmost visible graphic inside the column is the control's frame
				float best = float.MaxValue;
				foreach (var g in column.GetComponentsInChildren<Graphic>(false)) {
					if (g == null || g.transform == column) continue;
					var r = g.rectTransform;
					if (r.rect.width < 4f || r.rect.height < 4f) continue;
					float left = column.InverseTransformPoint(r.TransformPoint(new Vector3(r.rect.xMin, 0f, 0f))).x - column.rect.xMin;
					if (left >= -0.5f && left < best) best = left;
				}
				DescribeOnce(row, column);
				if (best < 400f) { ContentLeft = Mathf.Max(0f, best); UiLog.Once("contentleft", $"settings: native controls start {ContentLeft:0.#} units into the content column"); return; }
			}
		}
		catch (Exception e) { UiLog.Once("contentleft:err", "settings: could not measure native rows: " + e.Message); }
	}

	/// <summary>Logs the structure of one native row once, so layout problems can be diagnosed from LogOutput.log.</summary>
	private static void DescribeOnce(Transform row, RectTransform column) {
		if (_described) return;
		_described = true;
		var sb = new System.Text.StringBuilder();
		sb.Append($"native settings row '{row.name}': column rect={column.rect} anchors={column.anchorMin}-{column.anchorMax}");
		foreach (var c in column.GetComponents<Component>()) if (c != null) sb.Append("\n   column component: " + c.GetIl2CppType().Name);
		foreach (var r in column.GetComponentsInChildren<RectTransform>(true)) {
			if (r == null || r == column) continue;
			sb.Append($"\n   {r.name}: rect={r.rect} anchors={r.anchorMin}-{r.anchorMax} pos={r.anchoredPosition} pivot={r.pivot}");
		}
		UiLog.Info(sb.ToString());
	}
	private static bool _described;

	public static void AddHeader(GameSetInterface set, string name, string text) {
		var prefab = set.headerPrefab;
		if (prefab == null) { UiLog.Once("settings:header", "settings: headerPrefab missing"); return; }
		var header = UnityEngine.Object.Instantiate(prefab, set.holder);
		header.gameObject.name = name;
		SetLabel(header.visual, text);
	}

	/// <summary>A wrapped paragraph in the content column of a normal row (empty label), so it lines up with the game's own controls.</summary>
	public static void AddInfo(GameSetInterface set, string name, string text) {
		var entry = NewEntry(set, name, "");
		if (entry == null) return;
		var holder = entry.contentHolder;
		var label = NativeText.Create(holder, name + " Text", text, 16f, TextAlignmentOptions.TopLeft);
		label.enableWordWrapping = true;
		var rt = label.rectTransform;
		rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
		rt.offsetMin = new Vector2(ContentLeft, 0f); rt.offsetMax = Vector2.zero;

		float width = (holder.rect.width > 50f ? holder.rect.width : 420f) - ContentLeft;
		float height = label.GetPreferredValues(text ?? "", width, 0f).y + 8f;
		var le = entry.GetComponent<LayoutElement>() ?? entry.gameObject.AddComponent<LayoutElement>();
		le.minHeight = Mathf.Max(le.minHeight, height);
		le.preferredHeight = Mathf.Max(le.preferredHeight, height);
		Rebuild(set);
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

		TMP_Text value = null;   // shown between the buttons; both buttons refresh it at once instead of waiting for the live refresh
		NativeButton.CreateLabel(holder, name + " Minus", "-", 20f, () => { Step(step, -1); if (value != null) value.text = SafeText(valueText); })?.SetSize(StepButtonWidth, NativeButton.Height);

		value = NativeText.Create(holder, name + " Value", SafeText(valueText), 16f, TextAlignmentOptions.Center);
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
		h.childAlignment = TextAnchor.MiddleLeft;   // the game's own controls start at the left edge of the content column
		h.childControlWidth = false; h.childControlHeight = false;
		h.childForceExpandWidth = false; h.childForceExpandHeight = false;
		h.spacing = 6f;
		h.padding = new RectOffset((int)ContentLeft, 0, 0, 0);
	}

	private static void Rebuild(GameSetInterface set) {
		if (set.holder != null) LayoutRebuilder.MarkLayoutForRebuild(set.holder);
	}

	private static bool SafeGet(Func<bool> get) { try { return get != null && get(); } catch (Exception e) { UiLog.Once("settings:get", "settings getter: " + e.Message); return false; } }
	private static string SafeText(Func<string> f) { try { return f?.Invoke() ?? ""; } catch (Exception e) { UiLog.Once("settings:text", "settings value text: " + e.Message); return ""; } }
}
