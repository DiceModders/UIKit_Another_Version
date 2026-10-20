using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiceModders.UiKit;

/// <summary>
/// Reads the mouse wheel and the hover position for the widgets that need them (image elements, scroll views).
/// The game's own UI does not receive wheel events reliably, so the wheel is read straight from the Input System, but only while the pointer is over a
/// registered widget. With nothing registered the per-frame call returns after one list-count check.
/// </summary>
internal static class PointerTracker {
	internal sealed class Entry {
		public UnityEngine.Object Owner;
		public Func<Vector2, bool> Hover;           // screen point -> is the pointer over this widget
		public Action<float> Wheel;                  // notches (+ = up); null when the widget has no wheel use
		public Action<Vector2> Move;                 // called every frame while hovered (screen point)
	}

	private static readonly List<Entry> Entries = new();

	public static Entry Add(UnityEngine.Object owner, Func<Vector2, bool> hover, Action<float> wheel, Action<Vector2> move) {
		var e = new Entry { Owner = owner, Hover = hover, Wheel = wheel, Move = move };
		Entries.Add(e);
		return e;
	}

	public static void Remove(Entry e) { if (e != null) Entries.Remove(e); }

	/// <summary>Called every frame by the runner.</summary>
	public static void Frame() {
		if (Entries.Count == 0) return;
		try {
			var mouse = Mouse.current;
			if (mouse == null) return;
			Vector2 pos = mouse.position.ReadValue();
			float dy = mouse.scroll.ReadValue().y;
			float notches = dy == 0f ? 0f : (Mathf.Abs(dy) >= 20f ? dy / 120f : dy);

			bool wheelTaken = false;
			// Newest first: widgets created later sit on top of (or inside) earlier ones, so they get the wheel first.
			for (int i = Entries.Count - 1; i >= 0; i--) {
				var e = Entries[i];
				if (e.Owner == null) { Entries.RemoveAt(i); continue; }
				bool hovered;
				try { hovered = e.Hover(pos); }
				catch { hovered = false; }
				if (!hovered) continue;

				try { e.Move?.Invoke(pos); }
				catch (Exception ex) { UiLog.Once("pointer:move", "pointer move handler: " + ex); }

				if (notches != 0f && !wheelTaken && e.Wheel != null) {
					wheelTaken = true;
					try { e.Wheel(notches); }
					catch (Exception ex) { UiLog.Once("pointer:wheel", "wheel handler: " + ex); }
				}
			}
		}
		catch (Exception e) { UiLog.Once("pointer:frame", "pointer tracker: " + e.Message); }
	}

	/// <summary>The camera a screen-space point must be tested with for the canvas of <paramref name="rt"/> (null for overlay canvases).</summary>
	public static Camera CameraFor(RectTransform rt) {
		var canvas = rt != null ? rt.GetComponentInParent<Canvas>() : null;
		canvas = canvas != null ? canvas.rootCanvas : null;
		return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
	}
}
