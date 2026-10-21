using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

/// <summary>Handle of a hidden click area; <see cref="Remove"/> deletes it.</summary>
public sealed class SettingsHotspot {
	internal SettingsHotspot(string id, SettingsCorner corner, int clicks, Action trigger, float size, float gap) {
		Id = id; Corner = corner; Clicks = Math.Max(1, clicks); Trigger = trigger; Size = size; MaxGap = gap;
	}
	public string Id { get; }
	internal SettingsCorner Corner { get; }
	internal int Clicks { get; }
	internal Action Trigger { get; }
	internal float Size { get; }
	internal float MaxGap { get; }
	internal int Count;
	internal float Born;
	internal bool Described;
	internal float Last;
	internal readonly List<GameObject> Instances = new();

	internal void Click() {
		float now = Time.unscaledTime;
		float gap = now - Last;
		if (Count > 0 && gap > MaxGap) { UiLog.Info($"hotspot '{Id}' counter reset (gap {gap:0.0#}s > {MaxGap:0.0#}s)"); Count = 0; }
		Last = now;
		++Count;
		UiLog.Info($"hotspot '{Id}' click {Count}/{Clicks} (gap {gap:0.0#}s)");
		if (Count < Clicks) return;
		Count = 0;
		try { Trigger?.Invoke(); }
		catch (Exception e) { UiLog.Error($"hotspot '{Id}' failed: {e}"); }
	}

	public void Remove() => SettingsHotspots.Remove(this);
}

internal static class SettingsHotspots {
	private static readonly List<SettingsHotspot> Items = new();
	private static RectTransform _screen;
	private static Transform _close;

	/// <summary>Per frame: when the mouse is pressed over a hotspot, logs what the EventSystem finds on top there (diagnostics for hotspots that do not react).</summary>
	public static void Frame() {
		if (Items.Count == 0) return;
		try {
			foreach (var h in Items)
				if (!h.Described && h.Born > 0f && Time.unscaledTime - h.Born > 2f) {
					foreach (var go in h.Instances) if (go != null && go.activeInHierarchy) { h.Described = true; Describe(h, go); break; }
				}
			var mouse = UnityEngine.InputSystem.Mouse.current;
			if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
			var position = mouse.position.ReadValue();
			foreach (var h in Items) foreach (var go in h.Instances) {
				if (go == null || !go.activeInHierarchy) continue;
				var rt = go.GetComponent<RectTransform>();
				var canvas = go.GetComponentInParent<Canvas>();
				Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
				if (!RectTransformUtility.RectangleContainsScreenPoint(rt, position, cam)) continue;

				var system = UnityEngine.EventSystems.EventSystem.current;
				if (system == null) { UiLog.Info($"hotspot '{h.Id}': pressed, but there is no EventSystem"); return; }
				var data = new UnityEngine.EventSystems.PointerEventData(system);
				data.position = position;
				var hits = new Il2CppSystem.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
				system.RaycastAll(data, hits);
				string top = hits.Count > 0 && hits[0].gameObject != null ? hits[0].gameObject.name : "nothing";
				UiLog.Info($"hotspot '{h.Id}': mouse pressed on it; {hits.Count} raycast hits, topmost = '{top}'");
			}
		}
		catch (Exception e) { UiLog.Once("hotspot:probe", "hotspot probe: " + e.Message); }
	}

	/// <summary>Logs where a hotspot really is: its place in the hierarchy, size, world position, canvas and colour, plus its siblings and the screen's children.</summary>
	private static void Describe(SettingsHotspot h, GameObject go) {
		var rt = go.GetComponent<RectTransform>();
		var sb = new System.Text.StringBuilder($"hotspot '{h.Id}' state:");
		var path = new System.Collections.Generic.List<string>();
		for (var t = go.transform; t != null; t = t.parent) path.Add(t.name);
		path.Reverse();
		sb.Append("\n   path: " + string.Join(" / ", path));
		var corners = new Vector3[4];
		rt.GetWorldCorners(corners);
		var image = go.GetComponent<Image>();
		var canvas = go.GetComponent<Canvas>();
		var root = go.GetComponentInParent<Canvas>(true) != null ? go.GetComponentInParent<Canvas>(true).rootCanvas : null;
		sb.Append($"\n   rect={rt.rect} anchors={rt.anchorMin}-{rt.anchorMax} pos={rt.anchoredPosition} lossyScale={rt.lossyScale} world corners {corners[0]} {corners[2]}");
		sb.Append($"\n   active={go.activeInHierarchy} image={(image != null ? image.color.ToString() : "-")} raycast={(image != null && image.raycastTarget)} sibling={rt.GetSiblingIndex()}/{rt.parent.childCount}");
		sb.Append($"\n   own canvas: override={(canvas != null && canvas.overrideSorting)} order={(canvas != null ? canvas.sortingOrder : 0)}; root canvas: mode={(root != null ? root.renderMode.ToString() : "-")} order={(root != null ? root.sortingOrder : 0)}");
		var panel = rt.parent;
		if (panel != null) {
			sb.Append($"\n   panel '{panel.name}' children:");
			for (int i = 0; i < panel.childCount; i++) AppendNode(sb, panel.GetChild(i));
			var screen = panel.parent;
			if (screen != null) {
				sb.Append($"\n   '{screen.name}' children:");
				for (int i = 0; i < screen.childCount; i++) AppendNode(sb, screen.GetChild(i));
			}
		}
		UiLog.Info(sb.ToString());
	}

	private static void AppendNode(System.Text.StringBuilder sb, Transform t) {
		var r = t.GetComponent<RectTransform>();
		var c = t.GetComponent<Canvas>();
		sb.Append($"\n      {t.name}: active={t.gameObject.activeSelf} rect={(r != null ? r.rect.ToString() : "-")} anchors={(r != null ? r.anchorMin + "-" + r.anchorMax : "-")} canvas={(c != null ? "order " + c.sortingOrder + (c.overrideSorting ? " override" : "") : "-")}");
	}

	public static SettingsHotspot Add(string id, SettingsCorner corner, int clicks, Action trigger, float size, float gap) {
		if (string.IsNullOrEmpty(id)) throw new ArgumentException("A hotspot needs an id", nameof(id));
		foreach (var h in Items) if (h.Id == id) { UiLog.Warn($"hotspot '{id}' already exists - returning it"); return h; }
		var hotspot = new SettingsHotspot(id, corner, clicks, trigger, size, gap);
		Items.Add(hotspot);
		if (_screen != null && _screen.gameObject.activeInHierarchy) Create(hotspot, FindPanel(_screen, _close), "DM Hotspot " + id);   // the screen is already open
		return hotspot;
	}

	public static void Remove(SettingsHotspot hotspot) {
		if (!Items.Remove(hotspot)) return;
		foreach (var go in hotspot.Instances) if (go != null) UnityEngine.Object.Destroy(go);
		hotspot.Instances.Clear();
	}

	/// <summary>
	/// Creates any missing hotspot objects (called each time the Settings screen is shown or a tab is built). The corner belongs to the settings PANEL
	/// (the framed window the player sees), not to the screen root, which covers the whole display.
	/// </summary>
	public static void Ensure(RectTransform screen, Transform closeButton = null) {
		_screen = screen;
		_close = closeButton;
		var panel = FindPanel(screen, closeButton);
		foreach (var h in Items) {
			string name = "DM Hotspot " + h.Id;
			var existing = panel.Find(name);
			if (existing != null) { existing.SetAsLastSibling(); continue; }   // stay on top of whatever the game added since

			// created earlier under the screen root (before the panel had its size): move it into the panel
			GameObject old = null;
			foreach (var go in h.Instances) if (go != null) { old = go; break; }
			if (old != null) { Place(h, old.GetComponent<RectTransform>(), panel); continue; }
			Create(h, panel, name);
		}
	}

	/// <summary>
	/// The settings panel: the highest ancestor of the Close button that is still clearly smaller than the screen; when the Close button sits directly
	/// under the screen root, the largest other child of the screen that is smaller than the screen. Falls back to the screen itself until the layout is ready.
	/// </summary>
	private static RectTransform FindPanel(RectTransform screen, Transform close) {
		float full = screen.rect.width * screen.rect.height;
		if (full < 1f) return screen;   // still opening: no sizes yet

		RectTransform found = null;
		if (close != null) {
			for (var t = close; t != null && t.Pointer != screen.Pointer; t = t.parent) {
				var rt = t.TryCast<RectTransform>();
				if (rt != null && IsPanelSized(rt, full)) found = rt;
			}
		}
		if (found != null) return found;

		float best = 0f;
		for (int i = 0; i < screen.childCount; i++) {
			var child = screen.GetChild(i);
			if (child == null || child.name.StartsWith("DM") || (close != null && child.Pointer == close.Pointer)) continue;
			var rt = child.TryCast<RectTransform>();
			if (rt == null || !IsPanelSized(rt, full)) continue;
			float area = rt.rect.width * rt.rect.height;
			if (area > best) { best = area; found = rt; }
		}
		return found ?? screen;
	}

	private static bool IsPanelSized(RectTransform rt, float screenArea) =>
		rt.rect.width > 300f && rt.rect.height > 300f && rt.rect.width * rt.rect.height < screenArea * 0.9f;

	private static void Place(SettingsHotspot h, RectTransform rt, RectTransform panel) {
		if (rt.parent == null || rt.parent.Pointer != panel.Pointer) {
			rt.SetParent(panel, false);
			UiLog.Info($"hotspot '{h.Id}' placed in '{panel.name}' ({panel.rect.width:0}x{panel.rect.height:0}), corner {h.Corner}");
		}
		// The panel lays its children out itself (layout group), which would stretch the spot to a full-width row and overwrite the anchors below.
		// ignoreLayout is Unity's way of saying "this child positions itself".
		var element = rt.GetComponent<LayoutElement>();
		if (element == null) element = rt.gameObject.AddComponent<LayoutElement>();
		element.ignoreLayout = true;
		bool left = h.Corner == SettingsCorner.TopLeft || h.Corner == SettingsCorner.BottomLeft;
		bool top = h.Corner == SettingsCorner.TopLeft || h.Corner == SettingsCorner.TopRight;
		var anchor = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
		rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
		rt.anchoredPosition = Vector2.zero;
		rt.sizeDelta = new Vector2(h.Size, h.Size);
		rt.SetAsLastSibling();   // drawn and hit-tested after everything else in the panel
	}

	private static void Create(SettingsHotspot h, RectTransform screen, string name) {
		var rt = UiFactory.NewRect(name, screen);
		Place(h, rt, screen);

		var image = rt.gameObject.AddComponent<Image>();
		DebugView.Style(image, DebugView.Spot);   // visible (red) with [Debug] ShowInvisible
		// The settings pages may have canvases of their own, which are drawn and hit-tested above plain siblings. An override canvas on the spot puts it
		// above them (Unity's way of saying "this element is on top").
		var parentCanvas = screen.GetComponentInParent<Canvas>();
		var canvas = rt.gameObject.AddComponent<Canvas>();
		canvas.overrideSorting = true;
		canvas.sortingOrder = (parentCanvas != null ? parentCanvas.sortingOrder : 0) + 100;
		rt.gameObject.AddComponent<GraphicRaycaster>();
		var button = rt.gameObject.AddComponent<Button>();
		button.transition = Selectable.Transition.None;
		var action = Il2CppDelegates.ToUnityAction(() => h.Click());
		Il2CppDelegates.Hold(rt.gameObject, action);
		button.onClick.AddListener(action);
		h.Instances.Add(rt.gameObject);
		h.Born = Time.unscaledTime;
		UiLog.Info($"hotspot '{h.Id}' created in '{screen.name}' ({screen.rect.width:0}x{screen.rect.height:0}): {h.Corner}, {h.Size:0}x{h.Size:0}, needs {h.Clicks} clicks within {h.MaxGap:0.0#}s");
	}
}
