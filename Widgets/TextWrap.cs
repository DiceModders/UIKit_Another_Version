using System.Collections.Generic;

namespace DiceModders.UiKit;

/// <summary>Keeps the heights of auto-height labels (<c>Paragraph</c>, <c>Wrap</c>) in step with the width they get. Runs from the runner each frame.</summary>
internal static class TextWrap {
	private static readonly List<LabelHandle> Labels = new();

	public static void Track(LabelHandle label) { if (!Labels.Contains(label)) Labels.Add(label); }
	public static void Untrack(LabelHandle label) { Labels.Remove(label); }

	private sealed class Watched { public TMPro.TMP_Text Text; public float Width = -1f; }
	private static readonly List<Watched> Cut = new();

	/// <summary>
	/// A text that is cut with "..." (or shrunk) needs its mesh rebuilt once the layout has given it its real width; TMP does not always do that by itself
	/// for a text created before its first layout pass. Watches the width and rebuilds the text whenever it changes.
	/// </summary>
	public static void WatchWidth(TMPro.TMP_Text text) { if (text != null) Cut.Add(new Watched { Text = text }); }

	public static void Frame() {
		for (int i = Cut.Count - 1; i >= 0; i--) {
			var w = Cut[i];
			if (w.Text == null) { Cut.RemoveAt(i); continue; }
			float width = w.Text.rectTransform.rect.width;
			if (System.Math.Abs(width - w.Width) < 0.5f) continue;
			w.Width = width;
			w.Text.SetAllDirty();
			w.Text.ForceMeshUpdate();
		}
		ScrollHandle.PinAll();
		for (int i = Labels.Count - 1; i >= 0; i--) {
			var l = Labels[i];
			if (!l.Alive) { Labels.RemoveAt(i); continue; }
			try { l.Measure(false); }
			catch (System.Exception e) { UiLog.Once("textwrap", "text measure failed: " + e.Message); }
		}
	}
}
