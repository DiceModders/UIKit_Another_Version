using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

public sealed class ContainerHandle : ElementHandle<ContainerHandle> {
	internal ContainerHandle(RectTransform root) : base(root) { }

	public HorizontalOrVerticalLayoutGroup HVLayout => Root.GetComponent<HorizontalOrVerticalLayoutGroup>();
	public GridLayoutGroup GridLayout               => Root.GetComponent<GridLayoutGroup>();

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

		var handle = new LabelHandle(tmp, rt);
		handle.FixedH(fontSize * 1.4f);
		return handle;
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
			st.pressedSprite     = press;
			st.selectedSprite    = normal;
			st.disabledSprite    = Assets.ButtonBg(Assets.ButtonVariant.Regular, Assets.ButtonState.Disabled) ?? normal;
			btn.spriteState      = st;
		}

		UiFactory.AttachClick(btn, onClick);

		var label = UiFactory.MakeText(rt, "Label", text ?? "", fontSize, TextAlignmentOptions.Center);
		UiFactory.Stretch(label.rectTransform);
		label.raycastTarget = false;

		var handle = new ButtonHandle(rt, bg, label, btn);
		handle.FixedH(fontSize*2).WeightW(1f);
		return handle;
	}

	public ContainerHandle Space(float h = 8f) {
		var rt = UiFactory.NewRect("Space", Root);
		var le = rt.gameObject.AddComponent<LayoutElement>();
		le.preferredHeight = h;
		le.flexibleWidth = 0f;
		return this;
	}

	public ContainerHandle Separator(float h = 1f) {
		var rt = UiFactory.NewRect("Sep", Root);
		rt.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
		var le = rt.gameObject.AddComponent<LayoutElement>();
		le.preferredHeight = h;
		le.flexibleWidth = 0f;
		return this;
	}

	public ContainerHandle Vertical() => Vertical(null);

	public ContainerHandle Vertical(Action<ContainerHandle> build) {
		var rt = UiFactory.NewRect("V", Root);
		var lg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
		lg.spacing             = 6f;
		lg.padding             = new RectOffset(0, 0, 0, 0);
		lg.childAlignment      = TextAnchor.UpperLeft;
		lg.childControlWidth   = true;
		lg.childControlHeight  = true;
		lg.childForceExpandWidth  = true;
		lg.childForceExpandHeight = false;

		var handle = new ContainerHandle(rt);
		build?.Invoke(handle);
		return handle;
	}

	public ContainerHandle Horizontal() => Horizontal(null);

	public ContainerHandle Horizontal(Action<ContainerHandle> build) {
		var rt = UiFactory.NewRect("H", Root);
		var lg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
		lg.spacing             = 6f;
		lg.padding             = new RectOffset(0, 0, 0, 0);
		lg.childAlignment      = TextAnchor.UpperLeft;
		lg.childControlWidth   = true;
		lg.childControlHeight  = true;
		lg.childForceExpandWidth  = false;
		lg.childForceExpandHeight = true;

		var handle = new ContainerHandle(rt);
		build?.Invoke(handle);
		return handle;
	}

	public ContainerHandle Grid() => Grid(null);

	public ContainerHandle Grid(Action<ContainerHandle> build) {
		var rt = UiFactory.NewRect("Grid", Root);
		var lg = rt.gameObject.AddComponent<GridLayoutGroup>();
		lg.spacing        = new Vector2(6f, 6f);
		lg.padding        = new RectOffset(0, 0, 0, 0);
		lg.childAlignment = TextAnchor.UpperLeft;
		lg.constraint     = GridLayoutGroup.Constraint.Flexible;
		lg.cellSize       = new Vector2(100f, 100f);

		var handle = new ContainerHandle(rt);
		build?.Invoke(handle);
		return handle;
	}

	public ContainerHandle Spacing(float s) {
		if (HVLayout is { } hv) hv.spacing = s;
		if (GridLayout is { } g) g.spacing = new Vector2(s, s);
		return this;
	}

	public ContainerHandle Spacing(float x, float y) {
		if (GridLayout is { } g) g.spacing = new Vector2(x, y);
		var hv = HVLayout;
		if (hv is HorizontalLayoutGroup h) h.spacing = x;
		else if (hv is VerticalLayoutGroup v) v.spacing = y;
		return this;
	}

	public ContainerHandle Padding(int all)                    => Padding(all, all, all, all);
	public ContainerHandle Padding(int h, int v)               => Padding(h, h, v, v);
	public ContainerHandle Padding(int l, int r, int t, int b) => Padding(new RectOffset(l, r, t, b));

	public ContainerHandle Padding(RectOffset p) {
		if (p == null) return this;
		var copy = new RectOffset(p.left, p.right, p.top, p.bottom);
		if (HVLayout is { } hv) hv.padding = copy;
		if (GridLayout is { } g) g.padding = copy;
		return this;
	}

	public ContainerHandle Columns(int n) {
		if (GridLayout is { } g) {
			g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			g.constraintCount = n;
		}
		return this;
	}

	public ContainerHandle CellSize(float w, float h) {
		if (GridLayout is { } g) g.cellSize = new Vector2(w, h);
		return this;
	}

	public ContainerHandle CellSize(Vector2 size) => CellSize(size.x, size.y);
}
