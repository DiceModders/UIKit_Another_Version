using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceModders.UiKit;

public static class UiRoot {
	private static Canvas root;
	public static bool HostIsGame { get; private set; }

	public static Canvas Root {
		get {
			if (IsAlive(root)) return root;
			return Acquire();
		}
	}

	/// <summary>Forget the cached canvas (it is looked up again on next use).</summary>
	public static void Invalidate() { root = null; }

	private static bool IsAlive(Canvas c) {
		if (c == null) return false;
		var go = c.gameObject;
		return go != null && go.activeInHierarchy;
	}

	/// <summary>
	/// The game's HUD canvas: the root canvas that holds the top-right button bar. Cheap (a handful of canvases) and the same canvas the game's own
	/// windows live on, so our windows scale and sort like the game's.
	/// </summary>
	private static bool TryHud(out Canvas canvas) {
		canvas = null;
		try {
			foreach (var c in Object.FindObjectsOfType<Canvas>()) {
				if (c == null || !c.isRootCanvas || !c.gameObject.activeInHierarchy) continue;
				if (c.renderMode == RenderMode.WorldSpace || c.sortingOrder > 100) continue;
				if (GameUi.FindBar(c.transform) == null) continue;
				canvas = c;
				return true;
			}
		}
		catch { }
		return false;
	}

	private static bool TryAnchorLive(out Canvas canvas) {
		canvas = null;
		try {
			foreach (var t in Object.FindObjectsOfType<TMP_Text>()) {
				if (t == null || !t.gameObject.activeInHierarchy) continue;
				var c = t.GetComponentInParent<Canvas>();
				if (c == null || c.renderMode == RenderMode.WorldSpace) continue;
				var rc = c.rootCanvas;
				if (rc == null || rc.sortingOrder > 100) continue;   // skip debug overlays such as the game's [Graphy] performance canvas
				canvas = rc;
				return true;
			}
		}
		catch { }
		return false;
	}

	private static bool TryScan(out Canvas canvas) {
		canvas = null;
		try {
			Canvas[] all = Object.FindObjectsOfType<Canvas>(true);
			Canvas best = null;
			int bestScore = int.MinValue;
			foreach (var c in all) {
				if (c == null) continue;
				if (!c.gameObject.scene.IsValid()) continue;

				int score = 0;
				if (c.renderMode == RenderMode.ScreenSpaceOverlay) score += 1;
				if (c.isRootCanvas) score += 5;
				if (c.enabled) score += 1;
				if (c.gameObject.activeInHierarchy) score += 1;
				if (c.sortingOrder == 0) score += 1;
				if (c.sortingOrder > 100) score -= 5;

				if (score > bestScore) { bestScore = score; best = c; }
			}
			canvas = best;
			return canvas != null;
		}
		catch { }
		return false;
	}

	private static Canvas CreateOverlay() {
		var go = new GameObject("DiceModders UI");
		UnityEngine.Object.DontDestroyOnLoad(go);
		var rt = go.AddComponent<RectTransform>();
		rt.sizeDelta = new Vector2(1920f, 1080f);

		var c = go.AddComponent<Canvas>();
		c.renderMode = RenderMode.ScreenSpaceOverlay;
		c.sortingOrder = 500;

		var scaler = go.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 0.5f;

		go.AddComponent<GraphicRaycaster>();
		return c;
	}

	private static Canvas Acquire() {
		HostIsGame = true;
		if (TryHud(out var h)) root = h;
		else if (TryAnchorLive(out var a)) root = a;
		else if (TryScan(out var b)) root = b;
		else {
			UiLog.Warn("Can't find the game's canvas, creating an overlay canvas");
			HostIsGame = false;
			root = CreateOverlay();
		}
		return root;
	}
}
