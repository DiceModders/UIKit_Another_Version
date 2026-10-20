using System;
using System.Globalization;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameSettingFloat = DiceKingdoms.Meta.SettingInterfaceFloat;

namespace DiceModders.UiKit;

/// <summary>
/// A slider with a value text. It is a clone of the game's own float setting control (the Frame Rate Limit slider), so it has the game's art, handle and sounds;
/// the setting binding of the donor is removed, so nothing is written to the game's settings.
/// </summary>
public sealed class SliderHandle : ElementHandle<SliderHandle> {
	public Slider Slider { get; }
	public TMP_Text ValueText { get; }

	private readonly Action<float> _changed;
	private string _format;
	private bool _silent;

	internal SliderHandle(RectTransform root, Slider slider, TMP_Text valueText, string format, Action<float> changed) : base(root) {
		Slider = slider;
		ValueText = valueText;
		_format = string.IsNullOrEmpty(format) ? "0.##" : format;
		_changed = changed;

		Action<float> handler = OnSlider;
		var native = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<float>>(handler);
		slider.onValueChanged = new Slider.SliderEvent();   // drops listeners of the donor
		slider.onValueChanged.AddListener(native);
		Il2CppDelegates.Hold(root.gameObject, new object[] { handler, native });
		UpdateText();
	}

	public float Value => Slider != null ? Slider.value : 0f;

	/// <summary>Changes the value without calling the change callback.</summary>
	public SliderHandle SetValue(float value) {
		if (Slider == null) return this;
		_silent = true;
		try { Slider.value = value; UpdateText(); }
		finally { _silent = false; }
		return this;
	}

	public SliderHandle SetRange(float min, float max) {
		if (Slider == null) return this;
		_silent = true;
		try { Slider.minValue = min; Slider.maxValue = max; UpdateText(); }
		finally { _silent = false; }
		return this;
	}

	public SliderHandle WholeNumbers(bool on = true) { if (Slider != null) Slider.wholeNumbers = on; UpdateText(); return this; }

	/// <summary>.NET number format of the value text, e.g. "0", "0.0", "0.##", "P0" (percent).</summary>
	public SliderHandle Format(string format) { _format = string.IsNullOrEmpty(format) ? "0.##" : format; UpdateText(); return this; }

	public SliderHandle SetInteractable(bool on) { if (Slider != null) Slider.interactable = on; return this; }

	private void OnSlider(float value) {
		UpdateText();
		if (_silent) return;
		try { _changed?.Invoke(value); }
		catch (Exception e) { UiLog.Error("slider: " + e); }
		NativeButton.Deselect();
	}

	private void UpdateText() {
		if (ValueText == null || Slider == null) return;
		ValueText.text = Slider.value.ToString(_format, CultureInfo.InvariantCulture);
	}

	// ------------------------------------------------------------------ creation

	internal static SliderHandle Create(RectTransform parent, float min, float max, float value, Action<float> changed, string format) {
		var template = NativeTemplates.FloatSetting();
		if (template != null) {
			var h = FromNative(template, parent, min, max, value, changed, format);
			if (h != null) return h;
		}
		UiLog.Once("legacy-slider", "native slider template not found - using a hand-built slider.");
		return Fallback(parent, min, max, value, changed, format);
	}

	private static SliderHandle FromNative(GameSettingFloat template, RectTransform parent, float min, float max, float value, Action<float> changed, string format) {
		try {
			var go = UnityEngine.Object.Instantiate(template.gameObject, Staging.Root);   // parked: the donor's Awake (which binds it to a setting) has not run
			go.name = "DM Slider";
			NativeButton.StripForeign(go);
			var binding = go.GetComponent<GameSettingFloat>();
			if (binding != null) UnityEngine.Object.DestroyImmediate(binding);
			var slider = go.GetComponentInChildren<Slider>(true);
			if (slider == null) { UnityEngine.Object.Destroy(go); return null; }

			var rt = go.GetComponent<RectTransform>();
			float height = Mathf.Clamp(rt != null ? rt.rect.height : 32f, 24f, 48f);
			TMP_Text valueText = null;
			foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
				if (t != null) { valueText = t; break; }
			DescribeOnce(go);

			go.transform.SetParent(parent, false);
			go.SetActive(true);

			slider.minValue = min; slider.maxValue = max;
			slider.value = Mathf.Clamp(value, min, max);

			var handle = new SliderHandle(rt, slider, valueText, format, changed);
			handle.Layout.minWidth = 160f;
			handle.Layout.preferredHeight = handle.Layout.minHeight = height;
			handle.Layout.flexibleWidth = 1f;
			handle.Layout.flexibleHeight = 0f;
			return handle;
		}
		catch (Exception e) {
			UiLog.Warn("native slider failed: " + e.Message);
			return null;
		}
	}

	private static void DescribeOnce(GameObject go) {
		var sb = new System.Text.StringBuilder("native slider clone structure:");
		foreach (var r in go.GetComponentsInChildren<RectTransform>(true)) {
			if (r == null) continue;
			sb.Append($"\n   {r.name}: rect={r.rect} anchors={r.anchorMin}-{r.anchorMax} pos={r.anchoredPosition}");
		}
		UiLog.Once("slider-structure", sb.ToString());
	}

	private static SliderHandle Fallback(RectTransform parent, float min, float max, float value, Action<float> changed, string format) {
		var root = UiFactory.NewRect("DM Slider (fallback)", parent);

		var text = NativeText.Create(root, "DM Value", "", 14f, TextAlignmentOptions.Right);
		text.rectTransform.anchorMin = new Vector2(1f, 0f); text.rectTransform.anchorMax = new Vector2(1f, 1f);
		text.rectTransform.pivot = new Vector2(1f, 0.5f);
		text.rectTransform.sizeDelta = new Vector2(56f, 0f);
		text.rectTransform.anchoredPosition = Vector2.zero;

		var area = UiFactory.NewRect("DM Track", root);
		area.anchorMin = new Vector2(0f, 0f); area.anchorMax = new Vector2(1f, 1f);
		area.offsetMin = Vector2.zero; area.offsetMax = new Vector2(-64f, 0f);

		var bg = UiFactory.NewRect("Background", area);
		bg.anchorMin = new Vector2(0f, 0.5f); bg.anchorMax = new Vector2(1f, 0.5f);
		bg.sizeDelta = new Vector2(0f, 6f);
		bg.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

		var fillArea = UiFactory.NewRect("Fill Area", area);
		fillArea.anchorMin = new Vector2(0f, 0.5f); fillArea.anchorMax = new Vector2(1f, 0.5f);
		fillArea.sizeDelta = new Vector2(-10f, 6f);
		var fill = UiFactory.NewRect("Fill", fillArea);
		fill.gameObject.AddComponent<Image>().color = new Color(0.82f, 0.68f, 0.40f, 1f);
		fill.sizeDelta = new Vector2(10f, 0f);

		var slide = UiFactory.NewRect("Handle Slide Area", area);
		UiFactory.Stretch(slide);
		slide.offsetMin = new Vector2(5f, 0f); slide.offsetMax = new Vector2(-5f, 0f);
		var knob = UiFactory.NewRect("Handle", slide);
		knob.sizeDelta = new Vector2(12f, 22f);
		var knobImage = knob.gameObject.AddComponent<Image>();
		knobImage.color = new Color(0.95f, 0.90f, 0.78f, 1f);

		var slider = root.gameObject.AddComponent<Slider>();
		slider.fillRect = fill;
		slider.handleRect = knob;
		slider.targetGraphic = knobImage;
		slider.direction = Slider.Direction.LeftToRight;
		slider.minValue = min; slider.maxValue = max;
		slider.value = Mathf.Clamp(value, min, max);

		var handle = new SliderHandle(root, slider, text, format, changed);
		handle.Layout.minWidth = 160f;
		handle.Layout.preferredHeight = handle.Layout.minHeight = 32f;
		handle.Layout.flexibleWidth = 1f;
		return handle;
	}
}
