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

	public static GameObject AddHeader(GameSetInterface set, string name, string text) {
		var prefab = set.headerPrefab;
		if (prefab == null) { UiLog.Once("settings:header", "settings: headerPrefab missing"); return null; }
		var header = UnityEngine.Object.Instantiate(prefab, set.holder);
		header.gameObject.name = name;
		SetLabel(header.visual, text);
		return header.gameObject;
	}

	/// <summary>A wrapped paragraph in the content column of a normal row (empty label), so it lines up with the game's own controls.</summary>
	public static GameObject AddInfo(GameSetInterface set, string name, string text) => AddInfo(set, name, text, null);

	/// <summary>
	/// Like <see cref="AddInfo(GameSetInterface,string,string)"/>, but the text is re-read while the screen is open (four times a second); the row is hidden while
	/// the text is empty and grows or shrinks with it.
	/// </summary>
	public static GameObject AddLiveInfo(GameSetInterface set, string name, Func<string> text) => AddInfo(set, name, SafeText(text), text);

	private static GameObject AddInfo(GameSetInterface set, string name, string text, Func<string> live) {
		var entry = NewEntry(set, name, "");
		if (entry == null) return null;
		var holder = entry.contentHolder;
		var label = NativeText.Create(holder, name + " Text", text, 16f, TextAlignmentOptions.TopLeft);
		label.enableWordWrapping = true;
		var rt = label.rectTransform;
		rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
		rt.offsetMin = new Vector2(ContentLeft, 0f); rt.offsetMax = new Vector2(-ScrollbarReserve, 0f);   // keep clear of the scrollbar

		var le = entry.GetComponent<LayoutElement>() ?? entry.gameObject.AddComponent<LayoutElement>();
		void Fit(string value) {
			float width = ColumnWidth(holder) - ContentLeft;   // ColumnWidth already excludes the scrollbar
			float height = label.GetPreferredValues(value ?? "", width, 0f).y + 8f;
			le.minHeight = height;
			le.preferredHeight = height;
		}
		float first = label.GetPreferredValues(text ?? "", ColumnWidth(holder) - ContentLeft, 0f).y + 8f;
		le.minHeight = Mathf.Max(le.minHeight, first);
		le.preferredHeight = Mathf.Max(le.preferredHeight, first);
		if (live != null) {
			entry.gameObject.SetActive(!string.IsNullOrEmpty(text));
			string shown = text;
			SettingsLive.Add(entry, () => {
				string now = SafeText(live);
				if (now == shown) return;
				shown = now;
				label.text = now;
				Fit(now);
				entry.gameObject.SetActive(!string.IsNullOrEmpty(now));
				Rebuild(set);
			});
		}
		Rebuild(set);
		return entry.gameObject;
	}

	/// <param name="enabled">When given, the checkbox is only clickable while it returns true (re-read four times a second).</param>
	public static GameObject AddToggle(GameSetInterface set, string name, string label, Func<bool> get, Action<bool> setValue, Func<bool> enabled = null) {
		var entry = NewEntry(set, name, label);
		if (entry == null) return null;
		var holder = entry.contentHolder;
		EnsureRow(holder);   // same ContentLeft inset as buttons and steppers, so the checkbox lines up with the game's own controls
		var toggle = NativeToggle.Create(NativeTemplates.BoolSetting(), holder, SafeGet(get), v => {
			try { setValue?.Invoke(v); }
			catch (Exception e) { UiLog.Error($"settings toggle '{label}': {e}"); }
		});
		if (toggle == null) { UiLog.Once("settings:toggle", "settings: no native checkbox template found"); return entry.gameObject; }
		if (enabled != null) toggle.SetInteractable(SafeGet(enabled));
		SettingsLive.Add(entry, () => {
			toggle.SetSilently(SafeGet(get));
			if (enabled != null) toggle.SetInteractable(SafeGet(enabled));
		});
		Rebuild(set);
		return entry.gameObject;
	}

	public static void AddButtons(GameSetInterface set, string name, string label, (string text, Action onClick)[] buttons) {
		var entry = NewEntry(set, name, label);
		if (entry == null) return;
		var holder = entry.contentHolder;
		EnsureRow(holder);
		// every button gets at most an equal share of the column (minus the scrollbar), so nothing ever runs under it; longer texts shrink to fit
		int count = Mathf.Max(1, buttons.Length);
		float share = (ColumnWidth(holder) - ContentLeft - RowSpacing * (count - 1)) / count;
		foreach (var (text, onClick) in buttons) {
			var click = onClick;
			var b = NativeButton.CreateLabel(holder, name + " " + text, text, 16f, () => {
				try { click?.Invoke(); }
				catch (Exception e) { UiLog.Error($"settings button '{text}': {e}"); }
			});
			if (b == null) { UiLog.Once("settings:button", "settings: no native text button template found"); continue; }
			float natural = Mathf.Max(80f, 14f * (text?.Length ?? 0) + 24f);
			b.SetSize(Mathf.Min(natural, share), NativeButton.Height);
			if (b.Text != null) { b.Text.enableAutoSizing = true; b.Text.fontSizeMin = 10f; b.Text.fontSizeMax = 16f; b.Text.enableWordWrapping = false; b.Text.overflowMode = TextOverflowModes.Ellipsis; }
		}
		Rebuild(set);
	}

	/// <param name="enabled">When given, the "-" and "+" buttons are only clickable while it returns true (re-read four times a second).</param>
	public static GameObject AddStepper(GameSetInterface set, string name, string label, Func<string> valueText, Action<int> step, Func<bool> enabled = null) {
		var entry = NewEntry(set, name, label);
		if (entry == null) return null;
		var holder = entry.contentHolder;
		EnsureRow(holder);

		TMP_Text value = null;   // shown between the buttons; both buttons refresh it at once instead of waiting for the live refresh
		var minus = NativeButton.CreateLabel(holder, name + " Minus", "-", 20f, () => { Step(step, -1); if (value != null) value.text = SafeText(valueText); });
		minus?.SetSize(StepButtonWidth, NativeButton.Height);

		value = NativeText.Create(holder, name + " Value", SafeText(valueText), 16f, TextAlignmentOptions.Center);
		var le = value.gameObject.AddComponent<LayoutElement>();
		le.preferredWidth = ValueWidth; le.minWidth = ValueWidth; le.preferredHeight = NativeButton.Height;
		var plus = NativeButton.CreateLabel(holder, name + " Plus", "+", 20f, () => { Step(step, +1); value.text = SafeText(valueText); });
		plus?.SetSize(StepButtonWidth, NativeButton.Height);
		if (enabled != null) { bool on = SafeGet(enabled); minus?.SetInteractable(on); plus?.SetInteractable(on); }
		SettingsLive.Add(entry, () => {
			value.text = SafeText(valueText);
			if (enabled != null) { bool on = SafeGet(enabled); minus?.SetInteractable(on); plus?.SetInteractable(on); }
		});
		Rebuild(set);
		return entry.gameObject;
	}

	private const float RowSpacing = 6f;
	private const float ScrollbarReserve = 24f;   // the game's vertical scrollbar sits at the right edge of the column

	/// <summary>Usable width of a content column: its width minus the scrollbar (falls back to the native 500 when the layout has not run yet).</summary>
	private static float ColumnWidth(RectTransform holder) {
		float w = holder != null && holder.rect.width > 50f ? holder.rect.width : 500f;
		return w - ScrollbarReserve;
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
		var caption = entry.label?.visual;
		if (caption != null) { caption.enableWordWrapping = false; caption.overflowMode = TextOverflowModes.Ellipsis; }   // a long caption is cut with "..." instead of growing the row
		return entry;
	}

	private static void SetLabel(TMP_Text visual, string text) {
		if (visual != null) visual.text = text ?? "";
	}

	/// <summary>The game's content holder may already lay its children out; add a right aligned row only when it does not.</summary>
	private static void EnsureRow(RectTransform holder) {
		if (holder == null) return;
		var existing = holder.GetComponent<HorizontalOrVerticalLayoutGroup>();
		if (existing != null) {   // the game already lays this holder out: only make sure the inset is there
			if (existing.padding.left < (int)ContentLeft) existing.padding.left = (int)ContentLeft;
			return;
		}
		var h = holder.gameObject.AddComponent<HorizontalLayoutGroup>();
		h.childAlignment = TextAnchor.MiddleLeft;   // the game's own controls start at the left edge of the content column
		h.childControlWidth = false; h.childControlHeight = false;
		h.childForceExpandWidth = false; h.childForceExpandHeight = false;
		h.spacing = RowSpacing;
		h.padding = new RectOffset((int)ContentLeft, 0, 0, 0);
	}

	/// <summary>An empty row at the end of a tab, as tall as the game's Close button, so the last row can be scrolled clear of it.</summary>
	public static void AddBottomSpacer(GameSetInterface set, float height) {
		var rt = UiFactory.NewRect("DM Settings Spacer", set.holder);
		var le = rt.gameObject.AddComponent<LayoutElement>();
		le.minHeight = le.preferredHeight = height;
		le.flexibleWidth = 0f;
		Rebuild(set);
	}

	private static void Rebuild(GameSetInterface set) {
		if (set.holder != null) LayoutRebuilder.MarkLayoutForRebuild(set.holder);
	}

	private static bool SafeGet(Func<bool> get) { try { return get != null && get(); } catch (Exception e) { UiLog.Once("settings:get", "settings getter: " + e.Message); return false; } }
	private static string SafeText(Func<string> f) { try { return f?.Invoke() ?? ""; } catch (Exception e) { UiLog.Once("settings:text", "settings value text: " + e.Message); return ""; } }
}
