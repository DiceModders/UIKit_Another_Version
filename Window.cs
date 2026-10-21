using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Il2CppInterop.Runtime;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace DiceModders.UiKit;

public enum WindowState {
	Normal,
	Fullscreen,
}

internal sealed class WindowDragHandler : MonoBehaviour {
	public WindowDragHandler(IntPtr ptr) : base(ptr) { }

	public RectTransform WindowRoot;
	public WindowHandle Owner;
	public float ClampMinVisible = 8f;

	private bool _dragging;
	private bool _canDrag;
	private Canvas _canvas;

	private void Awake() {
		_canvas = GetComponentInParent<Canvas>();
		if (_canvas != null) _canvas = _canvas.rootCanvas;

		var trigger = gameObject.AddComponent<EventTrigger>();
		AddEntry(trigger, EventTriggerType.PointerDown,  OnPointerDown);
		AddEntry(trigger, EventTriggerType.BeginDrag,    OnBeginDrag);
		AddEntry(trigger, EventTriggerType.Drag,         OnDrag);
		AddEntry(trigger, EventTriggerType.PointerUp,    OnPointerUp);
		AddEntry(trigger, EventTriggerType.PointerClick, OnBackgroundClick);
	}

	private static void AddEntry(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action) {
		var entry = new EventTrigger.Entry { eventID = type };
		entry.callback.AddListener(DelegateSupport.ConvertDelegate<UnityAction<BaseEventData>>(action));
		trigger.triggers.Add(entry);
	}

	private static bool IsInteractive(GameObject hit) {
		if (hit == null) return false;
		return hit.GetComponentInParent<Selectable>() != null
			|| hit.GetComponentInParent<ScrollRect>() != null;
	}

	private void OnPointerDown(BaseEventData data) {
		var ped = data.TryCast<PointerEventData>();
		if (ped == null || ped.button != PointerEventData.InputButton.Left) return;

		Owner?.BringToFront();
		_canDrag = !IsInteractive(ped.pointerCurrentRaycast.gameObject);
	}

	private void OnBeginDrag(BaseEventData data) {
		_dragging = _canDrag;
	}

	private void OnDrag(BaseEventData data) {
		if (!_dragging || WindowRoot == null || Owner == null) return;
		if (Owner.State != WindowState.Normal) return;

		var ped = data.TryCast<PointerEventData>();
		if (ped == null) return;

		float s = _canvas != null ? _canvas.scaleFactor : 1f;
		if (s <= 0.0001f) s = 1f;

		var p = WindowRoot.anchoredPosition;
		p.x += ped.delta.x / s;
		p.y += ped.delta.y / s;

		ClampToParent(ref p);
		WindowRoot.anchoredPosition = p;
	}

	private void OnBackgroundClick(BaseEventData data) {
		var ped = data.TryCast<PointerEventData>();
		if (ped == null || ped.button != PointerEventData.InputButton.Left) return;
		if (Owner == null || ped.clickCount < 2) return;

		if (IsInteractive(ped.pointerCurrentRaycast.gameObject)) return;
		Owner.HandleDoubleClick();
	}

	private void OnPointerUp(BaseEventData data) {
		_dragging = false;
		_canDrag = false;
	}

	private void ClampToParent(ref Vector2 p) {
		var parent = WindowRoot.parent != null ? WindowRoot.parent.TryCast<RectTransform>() : null;   // 'as' gives null on IL2CPP wrappers
		if (parent == null) return;

		var pr = parent.rect;
		var wr = WindowRoot.sizeDelta;

		float minX = 0f;
		float maxX = Mathf.Max(0f, pr.width - wr.x);
		float minY = -Mathf.Max(0f, pr.height - ClampMinVisible);
		float maxY = 0f;

		p.x = Mathf.Clamp(p.x, minX, maxX);
		p.y = Mathf.Clamp(p.y, minY, maxY);
	}
}

internal sealed class WindowView {
	public RectTransform Root { get; private set; }
	public RectTransform BodyArea { get; private set; }
	public ContainerHandle Body { get; private set; }
	public Image OrnamentImage { get; private set; }
	private RectTransform _frame;
	private Image _frameOrnament;

	public const float TitleTop = 10f, TitleHeight = 32f, TitleGap = 6f;
	public RectTransform TitleStrip { get; private set; }
	public TMPro.TextMeshProUGUI TitleText { get; private set; }
	public ContainerHandle TitleButtons { get; private set; }

	private WindowView() { }

	public static WindowView Create(Transform canvas) {
		var v = new WindowView();
		v.BuildRoot(canvas);
		v.BuildBody();
		v.BuildOrnament();
		return v;
	}

	private void BuildRoot(Transform canvas) {
		Root = UiFactory.NewRect("DM Window", canvas);   // "DM " prefix: template searches never pick our own objects
		Root.anchorMin = Root.anchorMax = new Vector2(0f, 1f);
		Root.pivot = new Vector2(0f, 1f);

		// The game's own frame (background + ornament) when it can be found; otherwise the frame is built from the game's sprites.
		_frame = NativeFrame.Create(Root, out _frameOrnament);
		if (_frame != null) return;

		var bg = Root.gameObject.AddComponent<Image>();
		var sprite = Assets.PanelBg();
		if (sprite != null) {
			bg.sprite = sprite;
			bg.type = Image.Type.Sliced;
			bg.color = Color.white;
		}
		else bg.color = new Color(0.12f, 0.10f, 0.08f, 0.98f);
	}

	private void BuildBody() {
		BodyArea = UiFactory.NewRect("DM Body", Root);
		BodyArea.anchorMin = Vector2.zero;
		BodyArea.anchorMax = Vector2.one;
		BodyArea.offsetMin = new Vector2(WindowHandle.Padding, WindowHandle.Padding);
		BodyArea.offsetMax = new Vector2(-WindowHandle.Padding, -WindowHandle.Padding);

		BodyArea.gameObject.AddComponent<RectMask2D>();   // content that is taller than the window is cut at the body, it never spills out of the frame
		var vlg = BodyArea.gameObject.AddComponent<VerticalLayoutGroup>();
		vlg.childAlignment        = TextAnchor.UpperLeft;
		vlg.childControlWidth     = true;
		vlg.childControlHeight    = true;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = false;
		vlg.spacing               = 6f;
		vlg.padding               = new RectOffset(0, 0, 0, 0);

		Body = new ContainerHandle(BodyArea);
	}

	private void BuildOrnament() {
		if (_frameOrnament != null) {
			OrnamentImage = _frameOrnament;
			OrnamentImage.type = Image.Type.Sliced;
			OrnamentImage.raycastTarget = false;
			SetOrnament(Assets.OrnamentStyle.Border);
			return;
		}
        var rt = UiFactory.NewRect("DM Ornament", Root);
        UiFactory.Stretch(rt);

        OrnamentImage = rt.gameObject.AddComponent<Image>();
        OrnamentImage.type = Image.Type.Sliced;
        OrnamentImage.raycastTarget = false;
        OrnamentImage.enabled = false;

		SetOrnament(Assets.OrnamentStyle.Border);
    }

	/// <summary>Creates the title strip (title text on the left, a button row on the right) and moves the body below it. Does nothing the second time.</summary>
	public void EnsureTitle() {
		if (TitleStrip != null || Root == null) return;
		TitleStrip = UiFactory.NewRect("DM Title Bar", Root);
		TitleStrip.anchorMin = new Vector2(0f, 1f);
		TitleStrip.anchorMax = new Vector2(1f, 1f);
		TitleStrip.pivot = new Vector2(0.5f, 1f);
		TitleStrip.sizeDelta = new Vector2(0f, TitleHeight);
		SetTitleInsets(false);

		var lg = TitleStrip.gameObject.AddComponent<HorizontalLayoutGroup>();
		lg.spacing = 8f;
		lg.childAlignment = TextAnchor.MiddleLeft;
		lg.childControlWidth = true;
		lg.childControlHeight = true;
		lg.childForceExpandWidth = false;
		lg.childForceExpandHeight = true;

		TitleText = NativeText.Create(TitleStrip, "DM Title", "", 20f, TMPro.TextAlignmentOptions.MidlineLeft);
		TitleText.enableWordWrapping = false;
		TitleText.overflowMode = TMPro.TextOverflowModes.Ellipsis;   // cut with "..." instead of running under the buttons
		TextWrap.WatchWidth(TitleText);   // re-cut with "..." as soon as the layout has given it its width
		var tle = TitleText.gameObject.AddComponent<LayoutElement>();
		tle.minWidth = 0f; tle.preferredWidth = 0f; tle.flexibleWidth = 1f;

		var buttons = UiFactory.NewRect("DM Title Buttons", TitleStrip);
		var blg = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
		blg.spacing = 6f;
		blg.childAlignment = TextAnchor.MiddleRight;
		blg.childControlWidth = true;
		blg.childControlHeight = true;
		blg.childForceExpandWidth = false;
		blg.childForceExpandHeight = true;
		TitleButtons = new ContainerHandle(buttons) { TextSizedButtons = true };
		TitleButtons.Layout.flexibleWidth = 0f;

		BodyArea.offsetMax = new Vector2(-WindowHandle.Padding, -(TitleTop + TitleHeight + TitleGap));
	}

	/// <summary>The close X sits in the top-right corner (30 wide, 10 from the edge): with it the strip keeps clear of it.</summary>
	public void SetTitleInsets(bool closable) {
		if (TitleStrip == null) return;
		float right = closable ? 10f + NativeButton.SlicedFit + 8f : WindowHandle.Padding;
		TitleStrip.offsetMin = new Vector2(WindowHandle.Padding, -(TitleTop + TitleHeight));
		TitleStrip.offsetMax = new Vector2(-right, -TitleTop);
	}

	public void SetOrnament(Assets.OrnamentStyle style) {
        if (OrnamentImage == null) return;

        if (style==Assets.OrnamentStyle.None) {
            OrnamentImage.enabled = false;
            return;
        }

        var sprite = Assets.Ornament(style);
        if (sprite == null) {
            Plugin.Log?.LogWarning($"Ornament {style} not found, ornament disabled");
            OrnamentImage.enabled = false;
            return;
        }

        OrnamentImage.sprite = sprite;
        OrnamentImage.enabled = true;
    }

	public void ApplyNormal(Vector2 anchoredPosition, Vector2 sizeDelta) {
		if (Root == null) return;
		Root.anchorMin = Root.anchorMax = new Vector2(0f, 1f);
		Root.pivot = new Vector2(0f, 1f);
		Root.anchoredPosition = anchoredPosition;
		Root.sizeDelta = sizeDelta;
	}

	public void ApplyFullscreen() {
		if (Root == null) return;
		Root.anchorMin = Vector2.zero;
		Root.anchorMax = Vector2.one;
		Root.offsetMin = Vector2.zero;
		Root.offsetMax = Vector2.zero;
	}
}

public sealed class WindowHandle {
	public const float DefaultWidth  = 400f;
	public const float DefaultHeight = 260f;
	public const float Padding       = 16f;
	public const float MinWidth      = 160f;
	public const float MinHeight     = 100f;

	private WindowView _view;
	private WindowDragHandler _dragHandler;

	private float _width  = DefaultWidth;
	private float _height = DefaultHeight;
	private Vector2 _position;
	private bool _positioned;
	private bool _draggable, _fullscreenable;
	private bool _closed;
	private Assets.OrnamentStyle _ornamentStyle = Assets.OrnamentStyle.Border;
	private GameObject _closeButton;
	private ButtonHandle _fullscreenButton;

	private WindowState _state = WindowState.Normal;
	private Vector2 _normalAnchoredPosition, _normalSizeDelta;
	private bool _haveNormalSnapshot;

	public RectTransform Root     => _view?.Root;
	public ContainerHandle Body   => _view?.Body;
	public RectTransform BodyArea => _view?.BodyArea;

	public WindowState State                  => _state;
	public bool CanToggleFullscreen           => _fullscreenable;
	public bool IsOpen                        => !_closed && _view?.Root != null && _view.Root.gameObject != null;
    public Assets.OrnamentStyle OrnamentStyle => _ornamentStyle;

	private WindowHandle() { }

	internal static WindowHandle Create(Transform canvas) {
		var h = new WindowHandle();
		h._view = WindowView.Create(canvas);
		h.ApplyLayout();
		return h;
	}

	public WindowHandle Size(float width, float height) {
		_width  = Mathf.Max(MinWidth, width);
		_height = Mathf.Max(MinHeight, height);
		if (_haveNormalSnapshot) _normalSizeDelta = new Vector2(_width, _height);
		if (_state == WindowState.Normal) ApplyLayout();
		return this;
	}

	public WindowHandle Position(float x, float y) {
		_position = new Vector2(x, y);
		_positioned = true;
		if (_haveNormalSnapshot) _normalAnchoredPosition = new Vector2(x, -y);
		if (_state == WindowState.Normal) ApplyLayout();
		return this;
	}

	public WindowHandle Draggable(bool on = true) {
		_draggable = on;
		ApplyDraggable();
		return this;
	}

	public WindowHandle Fullscreenable(bool on = true) {
		_fullscreenable = on;
		return this;
	}

	/// <summary>
	/// Adds a title strip at the top (the game's font, left aligned, cut with "..." when too long) and moves the body below it.
	/// A window without <c>Title</c> looks as before. Use <see cref="TitleBar"/> for buttons on the right of the strip.
	/// </summary>
	public WindowHandle Title(string text) {
		if (!IsOpen) return this;
		_view.EnsureTitle();
		_view.SetTitleInsets(_closeButton != null);
		if (_view.TitleText != null) _view.TitleText.text = text ?? "";
		return this;
	}

	public WindowHandle SetTitle(string text) => Title(text);

	/// <summary>
	/// The right-hand part of the title strip: a horizontal container for buttons (<c>Button</c> there is as wide as its text; use
	/// <c>IconButton(...).FixedW(32)</c> for icons). Using it creates the strip, with an empty title. It keeps clear of the close X.
	/// </summary>
	public ContainerHandle TitleBar {
		get {
			if (!IsOpen) return null;
			_view.EnsureTitle();
			_view.SetTitleInsets(_closeButton != null);
			return _view.TitleButtons;
		}
	}

	/// <summary>Adds a "Full screen / Window" toggle button to the title bar (and makes the window fullscreenable). <c>FullscreenButton(false)</c> removes it.</summary>
	public WindowHandle FullscreenButton(bool on = true) {
		if (!IsOpen) return this;
		if (!on) {
			if (_fullscreenButton != null) UnityEngine.Object.Destroy(_fullscreenButton.Root.gameObject);
			_fullscreenButton = null;
			return this;
		}
		if (_fullscreenButton != null) return this;
		_fullscreenable = true;
		var bar = TitleBar;
		if (bar == null) return this;
		_fullscreenButton = bar.Button(_state == WindowState.Fullscreen ? "Window" : "Full screen", HandleDoubleClick);
		return this;
	}

    public WindowHandle Ornament(Assets.OrnamentStyle style = Assets.OrnamentStyle.Border) {
        _ornamentStyle   = style;
        _view?.SetOrnament(style);
        return this;
    }

	/// <summary>
	/// Puts the game's own close button (the small X of its About/Store windows, a clone) in the top-right corner; clicking it closes the window.
	/// Leave a little room at the top right of the body for it. <c>Closable(false)</c> removes it again.
	/// </summary>
	public WindowHandle Closable(bool on = true) {
		if (!IsOpen) return this;
		if (!on) {
			if (_closeButton != null) UnityEngine.Object.Destroy(_closeButton);
			_closeButton = null;
			_view.SetTitleInsets(false);
			return this;
		}
		if (_closeButton != null) return this;

		var native = NativeButton.CreateClose(_view.Root, "DM Close", Close);
		if (native == null) { UiLog.Warn("window: the game's close button template was not found - no X added."); return this; }
		_closeButton = native.Go;
		RectTransform rt = native.Rt;

		var le = _closeButton.GetComponent<UnityEngine.UI.LayoutElement>() ?? _closeButton.AddComponent<UnityEngine.UI.LayoutElement>();
		le.ignoreLayout = true;
		rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
		rt.pivot = new Vector2(1f, 1f);
		rt.sizeDelta = new Vector2(NativeButton.SlicedFit, NativeButton.SlicedFit);   // the same size as the title bar buttons
		rt.anchoredPosition = new Vector2(-10f, -10f);
		rt.SetAsLastSibling();
		_view.EnsureTitle();          // the X gets its own row above the body, so content never runs under it
		_view.SetTitleInsets(true);   // a title strip keeps clear of the X, whichever call came first
		return this;
	}

	public void Show() { if (IsOpen) _view.Root.gameObject.SetActive(true); }
	public void Hide() { if (IsOpen) _view.Root.gameObject.SetActive(false); }

	public void Close() {
		if (_closed) return;
		_closed = true;
		WindowRegistry.Unregister(this);
		if (_view?.Root != null && _view.Root.gameObject != null)
			UnityEngine.Object.Destroy(_view.Root.gameObject);
		_view = null;
		_dragHandler = null;
	}

	public void BringToFront() {
		if (!IsOpen) return;
		WindowRegistry.BringToFront(this);
	}

	internal void FinalizeBuild() {
		if (!_positioned) {
			int n = WindowRegistry.Count;
			Position(60f + n * 28f, 60f + n * 28f);
		}
		ApplyDraggable();
		WindowRegistry.Register(this);
	}

	public WindowHandle Fullscreen() { if (_fullscreenable) SetState(WindowState.Fullscreen); return this; }
	public WindowHandle Restore()    { SetState(WindowState.Normal); return this; }

	public WindowHandle SetState(WindowState state) {
		if (_state == state) return this;
		if (_state == WindowState.Normal) TakeSnapshot();
		_state = state;
		_fittedTo = default;   // the canvas may have changed while full screen
		ApplyStateVisual();
		return this;
	}

	internal void HandleDoubleClick() {
		if (!_fullscreenable) return;
		if (_state == WindowState.Fullscreen) Restore();
		else Fullscreen();
	}

	private Vector2 _fittedTo;

	/// <summary>
	/// Keeps a normal (not full screen) window fully inside the canvas. The canvas is not always 16:9 (4:3, 16:10, 21:9, a resized window...), so a
	/// position that was fine at the author's resolution can fall off the edge; the runner calls this whenever the canvas size changes.
	/// </summary>
	internal void FitToScreen(Vector2 canvasSize) {
		if (!IsOpen || _state != WindowState.Normal) return;
		if (canvasSize.x < 1f || canvasSize.y < 1f) return;
		var rt = _view.Root;
		var size = rt.sizeDelta;
		size.x = Mathf.Min(size.x, canvasSize.x);
		size.y = Mathf.Min(size.y, canvasSize.y);
		var p = rt.anchoredPosition;
		p.x = Mathf.Clamp(p.x, 0f, canvasSize.x - size.x);
		p.y = Mathf.Clamp(p.y, -(canvasSize.y - size.y), 0f);
		rt.sizeDelta = size;
		rt.anchoredPosition = p;
		_fittedTo = canvasSize;
	}

	internal bool IsFittedTo(Vector2 canvasSize) => _fittedTo == canvasSize;

	private void ApplyLayout() {
		if (_view == null) return;
		_view.ApplyNormal(new Vector2(_position.x, -_position.y), new Vector2(_width, _height));
	}

	private void ApplyDraggable() {
		if (_view?.Root == null) return;

		if (_draggable && _dragHandler == null) {
			_dragHandler = _view.Root.gameObject.AddComponent<WindowDragHandler>();
			_dragHandler.WindowRoot = _view.Root;
			_dragHandler.Owner = this;
			_dragHandler.ClampMinVisible = Padding;
		}
		else if (!_draggable && _dragHandler != null) {
			UnityEngine.Object.Destroy(_dragHandler);
			_dragHandler = null;
		}
	}

	private void TakeSnapshot() {
		if (_view?.Root == null) return;
		_normalAnchoredPosition = _view.Root.anchoredPosition;
		_normalSizeDelta = _view.Root.sizeDelta;
		_haveNormalSnapshot = true;
	}

	private void ApplyStateVisual() {
		if (_view == null) return;
		if (_fullscreenButton != null) _fullscreenButton.SetText(_state == WindowState.Fullscreen ? "Window" : "Full screen");

		switch (_state) {
			case WindowState.Normal:
				if (_haveNormalSnapshot)
					_view.ApplyNormal(_normalAnchoredPosition, _normalSizeDelta);
				else
					ApplyLayout();
				break;

			case WindowState.Fullscreen:
				_view.ApplyFullscreen();
				break;
		}
	}
}

internal static class WindowRegistry {
	private static readonly List<WindowHandle> Windows = new();
	public static int Count => Windows.Count;
	public static void Register(WindowHandle w)   { if (w != null && !Windows.Contains(w)) Windows.Add(w); }
	public static void Unregister(WindowHandle w) { Windows.Remove(w); }

	private static Vector2 _lastCanvas;

	/// <summary>Re-fits the windows when the canvas size changed since the last call (resolution, aspect ratio or window resize).</summary>
	public static void Tick() {
		if (Windows.Count == 0) return;
		var canvas = UiRoot.Root;
		var rt = canvas != null ? canvas.GetComponent<RectTransform>() : null;
		if (rt == null) return;
		var size = rt.rect.size;
		if (size != _lastCanvas) {
			_lastCanvas = size;
			LogCanvas(canvas, size);
		}
		for (int i = Windows.Count - 1; i >= 0; i--) {
			var w = Windows[i];
			if (!w.IsOpen) { Windows.RemoveAt(i); continue; }
			if (!w.IsFittedTo(size)) w.FitToScreen(size);
		}
	}

	private static void LogCanvas(Canvas canvas, Vector2 size) {
		var scaler = canvas.GetComponent<CanvasScaler>();
		string mode = scaler == null ? "no CanvasScaler" :
			$"{scaler.uiScaleMode}, reference {scaler.referenceResolution.x:0}x{scaler.referenceResolution.y:0}, {scaler.screenMatchMode} {scaler.matchWidthOrHeight:0.##}";
		UiLog.Info($"canvas '{canvas.name}': {size.x:0}x{size.y:0} units (ratio {size.x / size.y:0.00}), scale {canvas.scaleFactor:0.###}, screen {Screen.width}x{Screen.height}, {mode}");
	}

	public static void BringToFront(WindowHandle w) {
		if (w?.Root == null) return;
		w.Root.SetAsLastSibling();
		Windows.Remove(w);
		Windows.Add(w);
	}
}

