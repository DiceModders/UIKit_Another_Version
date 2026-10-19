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

	private static bool IsAlive(Canvas c) {
        if (c == null) return false;
        var go = c.gameObject;
        return go != null && go.activeInHierarchy;
    }

	private static bool TryAnchorLive(out Canvas canvas) {
        canvas = null;
        try {
            foreach (var t in UnityEngine.Object.FindObjectsOfType<TMP_Text>()) {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                var c = t.GetComponentInParent<Canvas>();
                if (c == null || c.renderMode == RenderMode.WorldSpace) continue;
                canvas = c.rootCanvas;
                if (canvas != null) return true;
            } 
        }
        catch {}
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

                if (score>bestScore) { bestScore = score; best = c; }
            }
            canvas = best;
            return canvas != null;
        }
        catch {}
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
        if (TryAnchorLive(out var a)) root = a;
		else if (TryScan(out var b)) root = b;
		else {
			Plugin.Log.LogWarning("Can't find canvas, creating it");
			root = CreateOverlay();
		}
		return root;
    }

}
