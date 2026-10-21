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
		throw new InvalidOperationException("UiKit: the game's slider template was not found (is the game's UI loaded?).");
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

			go.transform.SetParent(parent, false);
			go.SetActive(true);

			// the handle sticks out about half its width past the ends of the track, and the donor's track runs to the right edge of its row: pull it in so the handle is not cut off
			var track = slider.GetComponent<RectTransform>();
			if (track != null) track.offsetMax = new Vector2(track.offsetMax.x - 10f, track.offsetMax.y);

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
}
