using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

/// <summary>
/// A scrolling area. Content added to <see cref="Content"/> is laid out top to bottom; when it is taller than the area, a scrollbar of the game's own art
/// appears and the mouse wheel scrolls (the wheel is read directly, because the game's UI does not deliver wheel events reliably).
/// </summary>
public sealed class ScrollHandle : ElementHandle<ScrollHandle> {
	public ScrollRect Scroll { get; }
	public ContainerHandle Content { get; }

	private const float WheelStepPixels = 64f;
	private static readonly List<ScrollHandle> All = new();
	private bool _pinned = true;           // stays at the top while the content is being built / re-measured, until the user (or the mod) scrolls
	private float _lastContentHeight = -1f;

	internal ScrollHandle(RectTransform root, ScrollRect scroll, ContainerHandle content) : base(root) {
		Scroll = scroll;
		Content = content;
		All.Add(this);
		var viewport = scroll.viewport;
		PointerTracker.Add(root,
			p => root.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(viewport != null ? viewport : root, p, PointerTracker.CameraFor(root)),
			notches => Wheel(notches), null);
	}

	/// <summary>0 = top, 1 = bottom.</summary>
	public float Position {
		get => Scroll != null ? 1f - Scroll.verticalNormalizedPosition : 0f;
		set { _pinned = false; if (Scroll != null) Scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(value); }
	}

	/// <summary>
	/// A ScrollRect whose content grows after it was created (wrapped text, rows added later) would otherwise start at the bottom. Until someone scrolls,
	/// the view is kept at the top whenever the content height changes. Called each frame from <see cref="TextWrap"/>.
	/// </summary>
	internal static void PinAll() {
		for (int i = All.Count - 1; i >= 0; i--) {
			var h = All[i];
			if (h.Scroll == null || h.Scroll.content == null) { All.RemoveAt(i); continue; }
			h.Pin();
		}
	}

	private void Pin() {
		float height = Scroll.content.rect.height;
		bool changed = Mathf.Abs(height - _lastContentHeight) >= 0.5f;
		if (_pinned) {
			if (!changed) {
				if (Scroll.verticalNormalizedPosition < 0.999f && _lastContentHeight > 0f) _pinned = false;   // moved by the scrollbar or a drag
				return;
			}
			_lastContentHeight = height;
			Scroll.verticalNormalizedPosition = 1f;
			return;
		}
		if (!changed) return;
		_lastContentHeight = height;
		// The content got shorter or taller while scrolled (e.g. SetText on a paragraph): keep the pixel offset from the top but never beyond the new end,
		// otherwise the view can sit past the end of the content and show a half-empty area.
		float max = Mathf.Max(0f, height - (Scroll.viewport != null ? Scroll.viewport.rect.height : 0f));
		var pos = Scroll.content.anchoredPosition;
		float y = Mathf.Clamp(pos.y, 0f, max);
		if (Mathf.Abs(y - pos.y) > 0.01f) { pos.y = y; Scroll.content.anchoredPosition = pos; Scroll.StopMovement(); }
	}

	public ScrollHandle ScrollToTop()    { Position = 0f; return this; }
	public ScrollHandle ScrollToBottom() { Canvas.ForceUpdateCanvases(); Position = 1f; return this; }

	private void Wheel(float notches) {
		if (Scroll == null || Scroll.content == null || Scroll.viewport == null) return;
		float scrollable = Scroll.content.rect.height - Scroll.viewport.rect.height;
		if (scrollable <= 1f) return;
		_pinned = false;
		Scroll.verticalNormalizedPosition = Mathf.Clamp01(Scroll.verticalNormalizedPosition + notches * WheelStepPixels / scrollable);
	}

	// ------------------------------------------------------------------ creation

	internal static ScrollHandle Create(RectTransform parent, Action<ContainerHandle> build) {
		var root = UiFactory.NewRect("DM ScrollView", parent);
		var rootImage = root.gameObject.AddComponent<Image>();
		rootImage.color = new Color(0f, 0f, 0f, 0f);   // transparent, but catches drags
		var scroll = root.gameObject.AddComponent<ScrollRect>();
		scroll.horizontal = false;
		scroll.vertical = true;
		scroll.movementType = ScrollRect.MovementType.Clamped;
		scroll.inertia = true;
		scroll.scrollSensitivity = 0f;   // the wheel is handled by PointerTracker (once, whether or not the game delivers wheel events)

		var viewport = UiFactory.NewRect("DM Viewport", root);
		UiFactory.Stretch(viewport);
		viewport.gameObject.AddComponent<RectMask2D>();
		var vpImage = viewport.gameObject.AddComponent<Image>();
		vpImage.color = new Color(0f, 0f, 0f, 0f);

		var content = UiFactory.NewRect("DM Content", viewport);
		content.anchorMin = new Vector2(0f, 1f);
		content.anchorMax = new Vector2(1f, 1f);
		content.pivot = new Vector2(0.5f, 1f);
		content.offsetMin = new Vector2(0f, 0f);
		content.offsetMax = new Vector2(0f, 0f);
		var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
		vlg.spacing = 6f;
		vlg.childAlignment = TextAnchor.UpperLeft;
		vlg.childControlWidth = true;
		vlg.childControlHeight = true;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = false;
		content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		scroll.viewport = viewport;
		scroll.content = content;

		var bar = MakeScrollbar(root);
		if (bar != null) {
			scroll.verticalScrollbar = bar;
			scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
			scroll.verticalScrollbarSpacing = 4f;
		}

		var container = new ContainerHandle(content);
		var handle = new ScrollHandle(root, scroll, container);
		handle.Layout.minHeight = 60f;
		handle.Layout.flexibleWidth = 1f;
		handle.Layout.flexibleHeight = 1f;
		build?.Invoke(container);
		return handle;
	}

	private static Scrollbar MakeScrollbar(RectTransform root) {
		var template = NativeTemplates.VerticalScrollbar();
		if (template != null) {
			try {
				var go = UnityEngine.Object.Instantiate(template.gameObject, Staging.Root);
				go.name = "DM Scrollbar";
				NativeButton.StripForeign(go);
				var bar = go.GetComponent<Scrollbar>();
				if (bar != null) {
					bar.onValueChanged = new Scrollbar.ScrollEvent();
					bar.direction = Scrollbar.Direction.BottomToTop;
					var rt = go.GetComponent<RectTransform>();
					float width = Mathf.Clamp(rt.rect.width, 8f, 24f);
					go.transform.SetParent(root, false);
					rt.anchorMin = new Vector2(1f, 0f);
					rt.anchorMax = new Vector2(1f, 1f);
					rt.pivot = new Vector2(1f, 1f);
					rt.sizeDelta = new Vector2(width, 0f);
					rt.anchoredPosition = Vector2.zero;
					go.SetActive(true);
					return bar;
				}
				UnityEngine.Object.Destroy(go);
			}
			catch (Exception e) { UiLog.Warn("native scrollbar failed: " + e.Message); }
		}
		throw new InvalidOperationException("UiKit: the game's scrollbar template was not found (is the game's UI loaded?).");
	}
}
