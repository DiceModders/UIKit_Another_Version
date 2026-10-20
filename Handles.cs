using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

public sealed class LabelHandle : ElementHandle<LabelHandle> {
	public TextMeshProUGUI Text { get; }

	private bool _autoHeight;
	private float _lastWidth = -1f, _lastSize = -1f;
	private string _lastText;
	private float _lastHeight = -1f;

	internal LabelHandle(TextMeshProUGUI text, RectTransform root) : base(root) { Text = text; }

	public LabelHandle SetText(string value) {
		if (Text != null) Text.text = value ?? "";
		if (_autoHeight) Measure(true);
		return this;
	}
	public LabelHandle SetColor(Color c)     { if (Text != null) Text.color = c; return this; }
	public LabelHandle SetFontSize(float s)  { if (Text != null) Text.fontSize = s; if (_autoHeight) Measure(true); return this; }
	public string GetText() => Text != null ? Text.text : null;

	/// <summary>
	/// Word wrapping on/off. On: the text wraps at the width the label gets and its height follows the wrapped text (see <see cref="AutoHeight"/>);
	/// off (the default of <c>Label</c>): one line with a fixed height.
	/// </summary>
	public LabelHandle Wrap(bool on = true) {
		if (Text == null) return this;
		Text.enableWordWrapping = on;
		Text.overflowMode = on ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
		if (on) {
			var le = Layout;   // no pixel width of its own: it takes what the layout gives (and never pushes a row wider)
			le.minWidth = 0f; le.preferredWidth = 0f; le.flexibleWidth = 1f;
		}
		return AutoHeight(on);
	}

	/// <summary>
	/// On: the label's height is measured from the text and the width it actually gets, and re-measured when the width or the text changes.
	/// Off: the fixed one-line height of <c>Label</c> (font size x 1.4).
	/// </summary>
	public LabelHandle AutoHeight(bool on = true) {
		if (Text == null || on == _autoHeight) return this;
		_autoHeight = on;
		if (on) {
			var le = Layout;
			le.minHeight = 0f; le.preferredHeight = 0f; le.flexibleHeight = 0f;
			TextWrap.Track(this);
			Measure(true);
		}
		else {
			TextWrap.Untrack(this);
			FixedH(LineHeight(Text));
		}
		return this;
	}

	public LabelHandle Align(TextAlignmentOptions alignment) { if (Text != null) Text.alignment = alignment; return this; }
	public LabelHandle Left()   => Align(_autoHeight ? TextAlignmentOptions.TopLeft   : TextAlignmentOptions.Left);
	public LabelHandle Center() => Align(_autoHeight ? TextAlignmentOptions.Top       : TextAlignmentOptions.Center);
	public LabelHandle Right()  => Align(_autoHeight ? TextAlignmentOptions.TopRight  : TextAlignmentOptions.Right);

	/// <summary>Height of one line of the text. A text cut with "..." shows nothing at all when its box is shorter than a line, so the box must not be smaller than this.</summary>
	internal static float LineHeight(TMP_Text text) {
		float h = text.GetPreferredValues("Ag", 100000f, 0f).y;
		return Mathf.Ceil(Mathf.Max(h, text.fontSize * 1.5f)) + 1f;
	}

	internal bool Alive => Text != null && Root != null;

	/// <summary>Sets the label's height from its text at its current width. Cheap when nothing changed.</summary>
	internal void Measure(bool force) {
		if (!_autoHeight || Text == null || Root == null) return;
		float width = Root.rect.width;
		if (width < 1f) return;   // not laid out yet: the next frame tries again
		string text = Text.text;
		float size = Text.fontSize;
		if (!force && Mathf.Abs(width - _lastWidth) < 0.5f && size == _lastSize && string.Equals(text, _lastText)) return;
		_lastWidth = width; _lastSize = size; _lastText = text;

		float h = Mathf.Ceil(Text.GetPreferredValues(text, width, 0f).y);
		if (h < size * 1.2f) h = Mathf.Ceil(size * 1.2f);
		if (Mathf.Abs(h - _lastHeight) < 0.5f) return;
		_lastHeight = h;
		var le = Layout;
		le.minHeight = h; le.preferredHeight = h;
		var parent = Root.parent != null ? Root.parent.TryCast<RectTransform>() : null;
		if (parent != null) LayoutRebuilder.MarkLayoutForRebuild(parent);
	}
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

	internal ToggleHandle(RectTransform root, TextMeshProUGUI label, NativeToggle native) : base(root) {
		Label = label;
		_native = native;
	}

	public bool Value => _native.Value;

	/// <summary>Changes the state without calling the change callback.</summary>
	public ToggleHandle SetValue(bool value) {
		_native.SetSilently(value);
		return this;
	}

	public ToggleHandle SetText(string value) { if (Label != null) Label.text = value ?? ""; return this; }
	public ToggleHandle SetInteractable(bool on) { _native.SetInteractable(on); return this; }
}
