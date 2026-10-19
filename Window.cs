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
		var parent = WindowRoot.parent as RectTransform;
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

	private WindowView() { }

	public static WindowView Create(Transform canvas, WindowHandle owner) {
		var v = new WindowView();
		v.BuildRoot(canvas);
		v.BuildBody(owner);
		v.BuildOrnament();
		return v;
	}

	private void BuildRoot(Transform canvas) {
		Root = UiFactory.NewRect("Window", canvas);
		Root.anchorMin = Root.anchorMax = new Vector2(0f, 1f);
		Root.pivot = new Vector2(0f, 1f);

		var bg = Root.gameObject.AddComponent<Image>();
		var sprite = Assets.PanelBg();
		if (sprite != null) {
			bg.sprite = sprite;
			bg.type = Image.Type.Sliced;
			bg.color = Color.white;
		}
		else bg.color = new Color(0.12f, 0.10f, 0.08f, 0.98f);
	}

	private void BuildBody(WindowHandle owner) {
		BodyArea = UiFactory.NewRect("Body", Root);
		BodyArea.anchorMin = Vector2.zero;
		BodyArea.anchorMax = Vector2.one;
		BodyArea.offsetMin = new Vector2(WindowHandle.Padding, WindowHandle.Padding);
		BodyArea.offsetMax = new Vector2(-WindowHandle.Padding, -WindowHandle.Padding);

		var vlg = BodyArea.gameObject.AddComponent<VerticalLayoutGroup>();
		vlg.childAlignment = TextAnchor.UpperLeft;
		vlg.childControlWidth = true;
		vlg.childControlHeight = true;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = false;
		vlg.spacing = 6f;
		vlg.padding = new RectOffset(0, 0, 0, 0);

		Body = new ContainerHandle(BodyArea, owner);
	}

	private void BuildOrnament() {
        var rt = UiFactory.NewRect("Ornament", Root);
        UiFactory.Stretch(rt);

        OrnamentImage = rt.gameObject.AddComponent<Image>();
        OrnamentImage.type = Image.Type.Sliced;
        OrnamentImage.raycastTarget = false;
        OrnamentImage.enabled = false;

		SetOrnament(Assets.OrnamentStyle.Border);
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
		h._view = WindowView.Create(canvas, h);
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

    public WindowHandle Ornament(Assets.OrnamentStyle style = Assets.OrnamentStyle.Border) {
        _ornamentStyle   = style;
        _view?.SetOrnament(style);
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
		ApplyStateVisual();
		return this;
	}

	internal void HandleDoubleClick() {
		if (!_fullscreenable) return;
		if (_state == WindowState.Fullscreen) Restore();
		else Fullscreen();
	}

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

	public static void BringToFront(WindowHandle w) {
		if (w?.Root == null) return;
		w.Root.SetAsLastSibling();
		Windows.Remove(w);
		Windows.Add(w);
	}
}

