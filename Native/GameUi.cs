using UnityEngine;

namespace DiceModders.UiKit;

/// <summary>Where the game keeps the UI objects we reuse (verified against the game's hierarchy dump).</summary>
internal static class GameUi {
	// Top-right button bar: "Meta/Buttons" in a match, "Top Right/Top Right Buttons" in the main menu.
	public const string BarMatch = "Meta/Buttons";
	public const string BarMenu  = "Top Right/Top Right Buttons";
	public const string RulesButton = "Rules";
	public const string SettingsCloseButton = "Settings/Close Button";

	public static Transform FindBar(Transform canvasRoot) {
		if (canvasRoot == null) return null;
		return canvasRoot.Find(BarMatch) ?? canvasRoot.Find(BarMenu);
	}

	/// <summary>The first active top-right bar of any root canvas, or null while no scene with a bar is loaded.</summary>
	public static Transform FindActiveBar() {
		foreach (var canvas in Object.FindObjectsOfType<Canvas>(true)) {
			if (canvas == null || !canvas.isRootCanvas) continue;
			var bar = FindBar(canvas.transform);
			if (bar != null && bar.gameObject.activeInHierarchy) return bar;
		}
		return null;
	}
}
