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
		var tmp = NativeText.Create(Root, "DM Label", text, fontSize, TextAlignmentOptions.Left);
		var handle = new LabelHandle(tmp, tmp.rectTransform);
		handle.FixedH(fontSize * 1.4f);
		return handle;
	}

	/// <summary>
	/// A text button, cloned from the game's own Settings "Close" button: native art, hover/pressed states, cursor, sounds and shadow.
	/// The click goes through the game's own <c>BaseButton.onClick</c> event.
	/// </summary>
	public ButtonHandle Button(string text, Action onClick, float fontSize = 16f) {
		var native = NativeButton.CreateLabel(Root, "DM Button", text, fontSize, onClick);
		if (native == null) {
			UiLog.Once("legacy-button", "native text button template not found - using a hand-built button (no cursor change / sounds).");
			return LegacyButton(text, onClick, fontSize);
		}
		var handle = new ButtonHandle(native.Rt, native.Face, native.Text?.TryCast<TextMeshProUGUI>(), native.UnityButton, native);
		handle.FixedH(NativeButton.SlicedFit).WeightW(1f);
		return handle;
	}

	/// <summary>A square icon button, cloned from the game's top-right bar buttons. <paramref name="icon"/> is a sprite name of the game (see <see cref="Assets.SpriteNames"/>).</summary>
	public ButtonHandle IconButton(string icon, Action onClick, string tooltip = null)
		=> IconButton(Assets.GetSprite(icon), onClick, tooltip);

	public ButtonHandle IconButton(Sprite icon, Action onClick, string tooltip = null) {
		var native = NativeButton.CreateIcon(Root, "DM IconButton", icon, onClick, tooltip);
		if (native == null) {
			UiLog.Once("legacy-icon", "native icon button template not found - using a text button instead.");
			return LegacyButton(icon != null ? "*" : "?", onClick, 16f).Fixed(NativeButton.SlicedFit);
		}
		var handle = new ButtonHandle(native.Rt, native.Face, null, native.UnityButton, native);
		handle.Fixed(NativeButton.SlicedFit);
		return handle;
	}

	/// <summary>A checkbox in the game's own style (a clone of its boolean setting control) followed by a caption.</summary>
	public ToggleHandle Toggle(string text, bool value, Action<bool> onChanged, float fontSize = 16f) {
		var rt = UiFactory.NewRect("DM Toggle Row", Root);
		var lg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
		lg.spacing = 8f;
		lg.childAlignment = TextAnchor.MiddleLeft;
		lg.childControlWidth = true;
		lg.childControlHeight = true;
		lg.childForceExpandWidth = false;
		lg.childForceExpandHeight = false;

		var native = NativeToggle.Create(null, rt, value, onChanged);
		if (native == null) {
			UiLog.Once("legacy-toggle", "native checkbox template not found - using an On/Off button instead.");
			UnityEngine.Object.Destroy(rt.gameObject);
			ToggleHandle self = null;
			var b = LegacyButton($"{text}: {(value ? "On" : "Off")}", () => self?.Flip(), fontSize);
			self = new ToggleHandle(b.Root, b, text, value, onChanged);
			self.FixedH(fontSize * 2).WeightW(1f);
			return self;
		}

		var le = native.Go.GetComponent<LayoutElement>() ?? native.Go.AddComponent<LayoutElement>();
		le.minWidth = le.preferredWidth = 28f;
		le.minHeight = le.preferredHeight = 28f;
		var caption = new ContainerHandle(rt).Label(text, fontSize);
		caption.WeightW(1f);
		var handle = new ToggleHandle(rt, caption.Text, native);
		handle.FixedH(32f).WeightW(1f);
		return handle;
	}

	/// <summary>Hand-built fallback used only when the game's own button templates cannot be found (no cursor change, no sounds).</summary>
	private ButtonHandle LegacyButton(string text, Action onClick, float fontSize) {
		var rt = UiFactory.NewRect("DM Button (fallback)", Root);

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

		var label = UiFactory.MakeText(rt, "DM Label", text ?? "", fontSize, TextAlignmentOptions.Center);
		UiFactory.Stretch(label.rectTransform);
		label.raycastTarget = false;

		var handle = new ButtonHandle(rt, bg, label, btn);
		handle.FixedH(fontSize * 2).WeightW(1f);
		return handle;
	}

	public ContainerHandle Space(float h = 8f) {
		var rt = UiFactory.NewRect("DM Space", Root);
		var le = rt.gameObject.AddComponent<LayoutElement>();
		le.preferredHeight = h;
		le.flexibleWidth = 0f;
		return this;
	}

	public ContainerHandle Separator(float h = 1f) {
		var rt = UiFactory.NewRect("DM Sep", Root);
		rt.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
		var le = rt.gameObject.AddComponent<LayoutElement>();
		le.preferredHeight = h;
		le.flexibleWidth = 0f;
		return this;
	}

	public ContainerHandle Vertical() => Vertical(null);

	public ContainerHandle Vertical(Action<ContainerHandle> build) {
		var rt = UiFactory.NewRect("DM V", Root);
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
		var rt = UiFactory.NewRect("DM H", Root);
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
		var rt = UiFactory.NewRect("DM Grid", Root);
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
