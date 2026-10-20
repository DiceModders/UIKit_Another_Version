using System;
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

	internal ScrollHandle(RectTransform root, ScrollRect scroll, ContainerHandle content) : base(root) {
		Scroll = scroll;
		Content = content;
		var viewport = scroll.viewport;
		PointerTracker.Add(root,
			p => root.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(viewport != null ? viewport : root, p, PointerTracker.CameraFor(root)),
			notches => Wheel(notches), null);
	}

	/// <summary>0 = top, 1 = bottom.</summary>
	public float Position {
		get => Scroll != null ? 1f - Scroll.verticalNormalizedPosition : 0f;
		set { if (Scroll != null) Scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(value); }
	}

	public ScrollHandle ScrollToTop()    { Position = 0f; return this; }
	public ScrollHandle ScrollToBottom() { Canvas.ForceUpdateCanvases(); Position = 1f; return this; }

	private void Wheel(float notches) {
		if (Scroll == null || Scroll.content == null || Scroll.viewport == null) return;
		float scrollable = Scroll.content.rect.height - Scroll.viewport.rect.height;
		if (scrollable <= 1f) return;
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
		UiLog.Once("legacy-scrollbar", "native scrollbar template not found - using a hand-built scrollbar.");
		return FallbackScrollbar(root);
	}

	private static Scrollbar FallbackScrollbar(RectTransform root) {
		var rt = UiFactory.NewRect("DM Scrollbar (fallback)", root);
		rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 1f);
		rt.pivot = new Vector2(1f, 1f);
		rt.sizeDelta = new Vector2(12f, 0f);
		rt.anchoredPosition = Vector2.zero;
		rt.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);

		var area = UiFactory.NewRect("Sliding Area", rt);
		UiFactory.Stretch(area);
		var handle = UiFactory.NewRect("Handle", area);
		UiFactory.Stretch(handle);
		var handleImage = handle.gameObject.AddComponent<Image>();
		handleImage.color = new Color(0.82f, 0.68f, 0.40f, 0.85f);

		var bar = rt.gameObject.AddComponent<Scrollbar>();
		bar.handleRect = handle;
		bar.targetGraphic = handleImage;
		bar.direction = Scrollbar.Direction.BottomToTop;
		return bar;
	}
}
