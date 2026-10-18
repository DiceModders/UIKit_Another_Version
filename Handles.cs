using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

public sealed class LabelHandle {
    public TextMeshProUGUI Text { get; }
    internal LabelHandle(TextMeshProUGUI text) { Text = text; }

    public LabelHandle SetText(string value) { if (Text != null) Text.text = value ?? ""; return this; }
    public LabelHandle SetColor(Color c)     { if (Text != null) Text.color = c; return this; }
    public LabelHandle SetFontSize(float s)  { if (Text != null) Text.fontSize = s; return this; }
    public string GetText() => Text != null ? Text.text : null;
}

public sealed class ButtonHandle {
    public RectTransform Root { get; }
    public Image Background { get; }
    public TextMeshProUGUI Label { get; }

    internal ButtonHandle(RectTransform root, Image bg, TextMeshProUGUI label) {
        Root = root; Background = bg; Label = label;
    }

    public ButtonHandle SetText(string value) { if (Label != null) Label.text = value ?? ""; return this; }
    public ButtonHandle SetInteractable(bool on) {
        var b = Root != null ? Root.GetComponent<Button>() : null;
        if (b != null) b.interactable = on;
        return this;
    }
}
