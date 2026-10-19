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

	internal ButtonHandle(RectTransform root, Image bg, TextMeshProUGUI label, Button btn) : base(root) {
		Background = bg;
		Label = label;
		Button = btn;
	}

	public ButtonHandle SetText(string value) { if (Label != null) Label.text = value ?? ""; return this; }

	public ButtonHandle SetInteractable(bool on) {
		if (Button != null) Button.interactable = on;
		return this;
	}
}
