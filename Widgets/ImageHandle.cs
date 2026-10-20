using System;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceModders.UiKit;

public enum PointerKind { Down, Up, Click, Move, Drag, Enter, Exit }

/// <summary>A pointer event on an <see cref="ImageHandle"/>. Positions are inside the element: (0,0) is its bottom-left corner.</summary>
public readonly struct PointerEvent {
	public PointerKind Kind { get; }
	/// <summary>0 = left, 1 = right, 2 = middle (for Move/Enter/Exit: the button that is held, or -1).</summary>
	public int Button { get; }
	/// <summary>Position inside the element, 0..1 on both axes (may leave that range during a drag that started on the element).</summary>
	public Vector2 Uv { get; }
	/// <summary>Position inside the element in canvas units, from the bottom-left corner.</summary>
	public Vector2 Position { get; }
	/// <summary>Movement since the previous event in canvas units (Drag and Move).</summary>
	public Vector2 Delta { get; }

	public PointerEvent(PointerKind kind, int button, Vector2 uv, Vector2 position, Vector2 delta) {
		Kind = kind; Button = button; Uv = uv; Position = position; Delta = delta;
	}
}

/// <summary>
/// A picture on a window. Draw into a <see cref="Texture2D"/>, call <see cref="Refresh"/>, and receive pointer events (position, press, drag, hover)
/// and the mouse wheel. Useful for maps, minimaps and other custom drawings.
/// </summary>
public sealed class ImageHandle : ElementHandle<ImageHandle> {
	public RawImage Image { get; }

	private Action<PointerEvent> _pointer;
	private Action<float> _scroll;
	private bool _hover;
	private int _dragButton = -1;
	private Vector2 _lastLocal;
	private bool _haveLast;
	private PointerTracker.Entry _entry;
	private AspectRatioFitter _fitter;

	internal ImageHandle(RectTransform root, RawImage image) : base(root) {
		Image = image;
		// The element (Root) takes part in layouts; the picture sits inside it and keeps the texture's proportions, so pixels stay square in any container.
		_fitter = image.gameObject.AddComponent<AspectRatioFitter>();
		_fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
		UpdateRatio();
		Wire();
	}

	private void UpdateRatio() {
		if (_fitter == null) return;
		var t = Image != null ? Image.texture : null;
		_fitter.aspectRatio = t != null && t.height > 0 ? (float)t.width / t.height : 1f;
	}

	/// <summary>
	/// On by default: the picture keeps the proportions of its texture (square pixels stay square), centered in the space the element gets.
	/// Turn it off to stretch the picture over the whole element.
	/// </summary>
	public ImageHandle KeepAspect(bool on = true) {
		if (_fitter == null || Image == null) return this;
		_fitter.enabled = on;
		if (!on) UiFactory.Stretch(Image.rectTransform);
		else UpdateRatio();
		return this;
	}

	public Texture Texture => Image != null ? Image.texture : null;

	/// <summary>Size of the element in canvas units right now.</summary>
	public Vector2 Size => Image != null ? Image.rectTransform.rect.size : Vector2.zero;

	public ImageHandle SetTexture(Texture texture) {
		if (Image != null) Image.texture = texture;
		UpdateRatio();
		return this;
	}

	/// <summary>Point sampling keeps pixels sharp (maps, pixel art); bilinear smooths them.</summary>
	public ImageHandle Filter(FilterMode mode) {
		if (Image != null && Image.texture != null) Image.texture.filterMode = mode;
		return this;
	}

	/// <summary>Uploads the pixels written with SetPixels/SetPixelData to the GPU (call it after drawing).</summary>
	public ImageHandle Refresh() {
		var tex = Image != null ? Image.texture?.TryCast<Texture2D>() : null;
		if (tex != null) tex.Apply(false);
		return this;
	}

	/// <summary>Shows only a part of the texture (normalized 0..1 rectangle), e.g. for zooming and panning.</summary>
	public ImageHandle UvRect(Rect rect) {
		if (Image != null) Image.uvRect = rect;
		return this;
	}

	public ImageHandle Tint(Color color) {
		if (Image != null) Image.color = color;
		return this;
	}

	/// <summary>Called for pointer events on the picture. Replaces an earlier handler.</summary>
	public ImageHandle OnPointer(Action<PointerEvent> handler) { _pointer = handler; return this; }

	/// <summary>Called when the mouse wheel turns over the picture; the argument is the number of notches (positive = away from the user).</summary>
	public ImageHandle OnScroll(Action<float> handler) { _scroll = handler; return this; }

	// ------------------------------------------------------------------ wiring

	private void Wire() {
		var go = Image.gameObject;
		var trigger = go.AddComponent<EventTrigger>();
		var keep = new System.Collections.Generic.List<object>();
		void Add(EventTriggerType type, Action<BaseEventData> action) {
			var entry = new EventTrigger.Entry { eventID = type };
			var native = DelegateSupport.ConvertDelegate<UnityAction<BaseEventData>>(action);
			entry.callback.AddListener(native);
			trigger.triggers.Add(entry);
			keep.Add(action); keep.Add(native);
		}
		Add(EventTriggerType.PointerEnter, d => OnEnter(d));
		Add(EventTriggerType.PointerExit,  d => OnExit(d));
		Add(EventTriggerType.PointerDown,  d => OnDown(d));
		Add(EventTriggerType.PointerUp,    d => OnUp(d));
		Add(EventTriggerType.PointerClick, d => OnClick(d));
		Add(EventTriggerType.Drag,         d => OnDrag(d));
		Il2CppDelegates.Hold(go, keep);

		var rt = Image.rectTransform;
		_entry = PointerTracker.Add(rt, p => _hover && rt.gameObject.activeInHierarchy,
			notches => { if (_scroll != null) _scroll(notches); },
			pos => OnMove(pos));
	}

	private bool ToLocal(Vector2 screen, out Vector2 position, out Vector2 uv) {
		position = default; uv = default;
		if (Image == null) return false;
		var area = Image.rectTransform;
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, PointerTracker.CameraFor(area), out var local)) return false;
		var r = area.rect;
		position = local - r.min;
		uv = new Vector2(r.width > 0.001f ? position.x / r.width : 0f, r.height > 0.001f ? position.y / r.height : 0f);
		return true;
	}

	private void Raise(PointerKind kind, int button, Vector2 screen, bool useDelta) {
		if (_pointer == null || !ToLocal(screen, out var pos, out var uv)) return;
		Vector2 delta = Vector2.zero;
		if (useDelta && _haveLast) delta = pos - _lastLocal;
		_lastLocal = pos; _haveLast = true;
		try { _pointer(new PointerEvent(kind, button, uv, pos, delta)); }
		catch (Exception e) { UiLog.Error("image pointer handler: " + e); }
	}

	private static int Button(PointerEventData d) => (int)d.button;

	private void OnEnter(BaseEventData data) {
		var d = data.TryCast<PointerEventData>(); if (d == null) return;
		_hover = true; _haveLast = false;
		Raise(PointerKind.Enter, _dragButton, d.position, false);
	}

	private void OnExit(BaseEventData data) {
		var d = data.TryCast<PointerEventData>(); if (d == null) return;
		_hover = false;
		Raise(PointerKind.Exit, _dragButton, d.position, false);
	}

	private void OnDown(BaseEventData data) {
		var d = data.TryCast<PointerEventData>(); if (d == null) return;
		_dragButton = Button(d); _haveLast = false;
		Raise(PointerKind.Down, _dragButton, d.position, false);
	}

	private void OnUp(BaseEventData data) {
		var d = data.TryCast<PointerEventData>(); if (d == null) return;
		Raise(PointerKind.Up, Button(d), d.position, false);
		_dragButton = -1;
	}

	private void OnClick(BaseEventData data) {
		var d = data.TryCast<PointerEventData>(); if (d == null) return;
		Raise(PointerKind.Click, Button(d), d.position, false);
	}

	private void OnDrag(BaseEventData data) {
		var d = data.TryCast<PointerEventData>(); if (d == null) return;
		Raise(PointerKind.Drag, Button(d), d.position, true);
	}

	private void OnMove(Vector2 screen) {
		if (_pointer == null || _dragButton >= 0) return;   // while a button is held the Drag events report the movement
		if (!ToLocal(screen, out var pos, out _)) return;
		if (_haveLast && (pos - _lastLocal).sqrMagnitude < 0.0001f) return;
		Raise(PointerKind.Move, -1, screen, true);
	}
}
