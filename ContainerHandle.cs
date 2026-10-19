using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

public sealed class ContainerHandle {
    public RectTransform Root { get; }
	private readonly WindowHandle _owner;

    internal ContainerHandle(RectTransform root, WindowHandle owner = null) { 
		Root = root; 
		_owner = owner;
	}

    public LabelHandle Label(string text, float fontSize = 16f) {
        var rt = UiFactory.NewRect("Label", Root);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        var font = Assets.Font;
        if (font != null) tmp.font = font;
        var mat = Assets.FontShadowMaterial;
        if (mat != null) tmp.fontSharedMaterial = mat;
        tmp.text = text ?? string.Empty;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(0.969f, 0.940f, 0.893f, 1f);
        tmp.raycastTarget = false;

        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize * 1.4f;
        le.flexibleWidth = 1f;

        return new LabelHandle(tmp);
    }

    public ButtonHandle Button(string text, Action onClick, float fontSize = 16f) {
        var rt = UiFactory.NewRect("Button", Root);

        var bg = rt.gameObject.AddComponent<Image>();
        var normal = Assets.ButtonBg(Assets.ButtonVariant.Regular, Assets.ButtonState.Ready);
        var hover  = Assets.ButtonBg(Assets.ButtonVariant.Regular, Assets.ButtonState.Hovered);
        var press  = Assets.ButtonBg(Assets.ButtonVariant.Regular, Assets.ButtonState.Pressed);
        if (normal != null) {
            bg.sprite = normal;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }
        else bg.color = new Color(0.20f, 0.17f, 0.13f, 0.95f);

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;

        if (normal != null && hover != null && press != null) {
            btn.transition = Selectable.Transition.SpriteSwap;
            var st = btn.spriteState;
            st.highlightedSprite = hover;
            st.pressedSprite = press;
            st.selectedSprite = normal;
            st.disabledSprite = Assets.ButtonBg(Assets.ButtonVariant.Regular, Assets.ButtonState.Disabled) ?? normal;
            btn.spriteState = st;
        }

        UiFactory.AttachClick(btn, onClick);

        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 30f;
        le.flexibleWidth = 1f;

        var label = UiFactory.MakeText(rt, "Label", text ?? "", fontSize, TextAlignmentOptions.Center);
        UiFactory.Stretch(label.rectTransform);

        return new ButtonHandle(rt, bg, label);
    }

    public ContainerHandle Vertical()  => throw new NotImplementedException("phase 4");
    public ContainerHandle Horizontal()=> throw new NotImplementedException("phase 4");
    public ContainerHandle Grid(int cols) => throw new NotImplementedException("phase 4");
}
