using System;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DiceModders.UiKit;

internal sealed class WindowDragHandler : MonoBehaviour {
    public RectTransform WindowRoot;
    public WindowHandle Owner;
    public float TitleBarHeight = 32f;

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
		AddEntry(trigger, EventTriggerType.PointerClick, OnTitleBarClick);
	}

    private static void AddEntry(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action) {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(DelegateSupport.ConvertDelegate<UnityAction<BaseEventData>>(action));
        trigger.triggers.Add(entry);
    }

    private void OnPointerDown(BaseEventData data) {
        var ped = data.TryCast<PointerEventData>();
        if (ped == null || ped.button != PointerEventData.InputButton.Left) return;

        Owner?.BringToFront();

        var hit = ped.pointerCurrentRaycast.gameObject;
        _canDrag = hit == null || hit.GetComponentInParent<Selectable>() == null;
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

	private void OnTitleBarClick(BaseEventData data) {
		var ped = data.TryCast<PointerEventData>();
		if (ped == null || ped.button != PointerEventData.InputButton.Left) return;
		if (Owner == null || ped.clickCount < 2) return;

		if (Owner.State == WindowState.Minimized)
			Owner.Restore();
		else if (Owner.CanToggleFullscreen)
			Owner.ToggleFullscreenFromDoubleClick();
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
        float minY = -Mathf.Max(0f, pr.height - TitleBarHeight);
        float maxY = 0f;

        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
    }
}
