using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

public sealed class LabelHandle : ElementHandle<LabelHandle> {
	public TextMeshProUGUI Text { get; }

	internal LabelHandle(TextMeshProUGUI text, RectTransform root) : base(root) { Text = text; }

	public LabelHandle SetText(string value) { if (Text != null) Text.text = value ?? ""; return this; }
	public LabelHandle SetColor(Color c)     { if (Text != null) Text.color = c; return this; }
	public LabelHandle SetFontSize(float s)  { if (Text != null) Text.fontSize = s; return this; }
	public string GetText() => Text != null ? Text.text : null;
}

public sealed class ButtonHandle : ElementHandle<ButtonHandle> {
	public Image Background { get; }
	public TextMeshProUGUI Label { get; }
	public Button Button { get; }

	/// <summary>The cloned game button behind this handle (null when the library had to fall back to a hand-built button).</summary>
	internal NativeButton Native { get; }

	internal ButtonHandle(RectTransform root, Image bg, TextMeshProUGUI label, Button btn, NativeButton native = null) : base(root) {
		Background = bg;
		Label = label;
		Button = btn;
		Native = native;
	}

	public ButtonHandle SetText(string value) { if (Label != null) Label.text = value ?? ""; return this; }

	public ButtonHandle SetInteractable(bool on) {
		if (Native != null) Native.SetInteractable(on);
		else if (Button != null) Button.interactable = on;
		return this;
	}

	/// <summary>Replaces the click handler (the game's own <c>BaseButton.onClick</c> event for native buttons).</summary>
	public ButtonHandle OnClick(Action onClick) {
		if (Native != null) Native.SetClick(onClick);
		else if (Button != null) {
			Button.onClick = new Button.ButtonClickedEvent();
			UiFactory.AttachClick(Button, onClick);
		}
		return this;
	}

	/// <summary>
	/// Selected buttons show the game's normal bright art, unselected ones the dark "disabled" art (like the Settings tabs);
	/// hover and press still show the game's own hover/pressed art. Buttons are selected by default.
	/// </summary>
	public ButtonHandle SetSelected(bool selected) {
		if (Native != null) Native.SetSelected(selected);
		else if (Background != null) Background.color = selected ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
		return this;
	}

	/// <summary>Changes the game icon of an icon button.</summary>
	public ButtonHandle SetIcon(Sprite sprite) { Native?.SetIcon(sprite); return this; }

	/// <summary>Changes the tooltip text (the button must have been created with a tooltip).</summary>
	public ButtonHandle SetTooltip(string text) { Native?.SetTooltip(text); return this; }
}

public sealed class ToggleHandle : ElementHandle<ToggleHandle> {
	public TextMeshProUGUI Label { get; }
	private readonly NativeToggle _native;
	private readonly ButtonHandle _fallback;
	private readonly Action<bool> _changed;
	private bool _fallbackValue;
	private readonly string _fallbackText;

	internal ToggleHandle(RectTransform root, TextMeshProUGUI label, NativeToggle native) : base(root) {
		Label = label;
		_native = native;
	}

	internal ToggleHandle(RectTransform root, ButtonHandle fallback, string text, bool value, Action<bool> changed) : base(root) {
		_fallback = fallback;
		_fallbackText = text;
		_fallbackValue = value;
		_changed = changed;
		Label = fallback?.Label;
	}

	public bool Value => _native != null ? _native.Value : _fallbackValue;

	/// <summary>Changes the state without calling the change callback.</summary>
	public ToggleHandle SetValue(bool value) {
		if (_native != null) _native.SetSilently(value);
		else { _fallbackValue = value; _fallback?.SetText($"{_fallbackText}: {(value ? "On" : "Off")}"); }
		return this;
	}

	public ToggleHandle SetText(string value) { if (Label != null && _native != null) Label.text = value ?? ""; return this; }
	public ToggleHandle SetInteractable(bool on) { _native?.SetInteractable(on); _fallback?.SetInteractable(on); return this; }

	internal void Flip() {
		_fallbackValue = !_fallbackValue;
		_fallback?.SetText($"{_fallbackText}: {(_fallbackValue ? "On" : "Off")}");
		_changed?.Invoke(_fallbackValue);
	}
}
