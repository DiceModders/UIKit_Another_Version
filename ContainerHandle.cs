using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

public sealed class ContainerHandle : ElementHandle<ContainerHandle> {
	internal ContainerHandle(RectTransform root) : base(root) { }

	/// <summary>Set for a window's title bar: buttons there are as wide as their text instead of sharing the row.</summary>
	internal bool TextSizedButtons;

	public HorizontalOrVerticalLayoutGroup HVLayout => Root.GetComponent<HorizontalOrVerticalLayoutGroup>();
	public GridLayoutGroup GridLayout               => Root.GetComponent<GridLayoutGroup>();

	public LabelHandle Label(string text, float fontSize = 16f) {
		var tmp = NativeText.Create(Root, "DM Label", text, fontSize, TextAlignmentOptions.Left);
		tmp.enableWordWrapping = false;                      // one line of fixed height: a long text is cut with "..." (use Paragraph / Wrap() to wrap it)
		tmp.overflowMode = TextOverflowModes.Ellipsis;
		TextWrap.WatchWidth(tmp);                            // re-cut once the layout has given it its real width
		var handle = new LabelHandle(tmp, tmp.rectTransform);
		handle.FixedH(LabelHandle.LineHeight(tmp));
		return handle;
	}

	/// <summary>
	/// Text that wraps at the width it gets and is as tall as the wrapped text (top-left aligned). It needs no fixed width, works in a window body, a
	/// <c>Vertical</c> or a scroll view, and re-measures when the width changes or <see cref="LabelHandle.SetText"/> is called.
	/// </summary>
	public LabelHandle Paragraph(string text, float fontSize = 16f) {
		var tmp = NativeText.Create(Root, "DM Paragraph", text, fontSize, TextAlignmentOptions.TopLeft);
		var handle = new LabelHandle(tmp, tmp.rectTransform);
		return handle.Wrap(true);
	}

	/// <summary>
	/// A text button, cloned from the game's own Settings "Close" button: native art, hover/pressed states, cursor, sounds and shadow.
	/// The click goes through the game's own <c>BaseButton.onClick</c> event.
	/// </summary>
	public ButtonHandle Button(string text, Action onClick, float fontSize = 16f) {
		var native = NativeButton.CreateLabel(Root, "DM Button", text, fontSize, onClick);
		if (native == null) throw new InvalidOperationException("UiKit: the game's button template was not found (is the game's UI loaded?).");
		var handle = new ButtonHandle(native.Rt, native.Face, native.Text?.TryCast<TextMeshProUGUI>(), native.UnityButton, native);
		handle.FixedH(NativeButton.SlicedFit);
		if (TextSizedButtons) handle.FixedW(Mathf.Max(64f, Mathf.Ceil(handle.Label != null ? handle.Label.GetPreferredValues(text ?? "").x : 0f) + 28f));
		else handle.WeightW(1f);
		return handle;
	}

	/// <summary>A square icon button, cloned from the game's top-right bar buttons. <paramref name="icon"/> is a sprite name of the game (see <see cref="Assets.SpriteNames"/>).</summary>
	public ButtonHandle IconButton(string icon, Action onClick, string tooltip = null)
		=> IconButton(Assets.GetSprite(icon), onClick, tooltip);

	public ButtonHandle IconButton(Sprite icon, Action onClick, string tooltip = null) {
		var native = NativeButton.CreateIcon(Root, "DM IconButton", icon, onClick, tooltip);
		if (native == null) throw new InvalidOperationException("UiKit: the game's icon button template was not found (is the game's UI loaded?).");
		var handle = new ButtonHandle(native.Rt, native.Face, null, native.UnityButton, native);
		native.FitIcon(NativeButton.SlicedFit);
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
		if (native == null) { UnityEngine.Object.Destroy(rt.gameObject); throw new InvalidOperationException("UiKit: the game's checkbox template was not found (is the game's UI loaded?)."); }

		var le = native.Go.GetComponent<LayoutElement>() ?? native.Go.AddComponent<LayoutElement>();
		le.minWidth = le.preferredWidth = 28f;
		le.minHeight = le.preferredHeight = 28f;
		var caption = new ContainerHandle(rt).Label(text, fontSize);
		caption.WeightW(1f);
		var handle = new ToggleHandle(rt, caption.Text, native);
		handle.FixedH(32f).WeightW(1f);
		return handle;
	}

	/// <summary>
	/// A picture (a RawImage) that fills the available space but keeps its texture's proportions (square pixels stay square; see <see cref="ImageHandle.KeepAspect"/>); use <c>FixedW/FixedH</c> or <c>Fixed</c> for a set size. Draw into the texture and call
	/// <see cref="ImageHandle.Refresh"/>; <see cref="ImageHandle.OnPointer"/> and <see cref="ImageHandle.OnScroll"/> give position, press, drag, hover and wheel.
	/// </summary>
	public ImageHandle Image(Texture texture = null) {
		var rt = UiFactory.NewRect("DM Image", Root);
		var picture = UiFactory.NewRect("DM Picture", rt);
		var raw = picture.gameObject.AddComponent<RawImage>();
		raw.texture = texture;
		raw.raycastTarget = true;
		var handle = new ImageHandle(rt, raw);
		handle.Layout.minWidth = 16f;
		handle.Layout.minHeight = 16f;
		handle.Layout.flexibleWidth = 1f;
		handle.Layout.flexibleHeight = 1f;
		return handle;
	}

	/// <summary>A slider with a value text, in the game's own style (a clone of its Frame Rate Limit slider).</summary>
	/// <param name="format">.NET number format of the value text, e.g. "0", "0.0", "0.##".</param>
	public SliderHandle Slider(float min, float max, float value, Action<float> onChanged, string format = "0.##")
		=> SliderHandle.Create(Root, min, max, value, onChanged, format);

	/// <summary>A drop-down list in the game's own style (a clone of its Language dropdown). <paramref name="onChanged"/> gets the chosen index.</summary>
	public DropdownHandle Dropdown(System.Collections.Generic.IEnumerable<string> options, int index, Action<int> onChanged)
		=> DropdownHandle.Create(Root, options, index, onChanged);

	/// <summary>
	/// A scrolling area: <paramref name="build"/> fills its <see cref="ScrollHandle.Content"/>. A scrollbar of the game's art appears when the content is taller
	/// than the area, and the mouse wheel scrolls. Give it a height with <c>FixedH</c>, or let it fill the space left in a vertical container.
	/// </summary>
	public ScrollHandle ScrollView(Action<ContainerHandle> build)
		=> ScrollHandle.Create(Root, build);

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
