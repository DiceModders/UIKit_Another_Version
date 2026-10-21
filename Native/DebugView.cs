using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

/// <summary>
/// The invisible raycast graphics UiKit adds (hidden click spots, the scroll catcher) are drawn with these colours. With the config entry
/// [Debug] ShowInvisible they become visible, so "where is the spot?" can be answered by looking.
/// </summary>
internal static class DebugView {
	public static bool On => Plugin.ShowInvisible != null && Plugin.ShowInvisible.Value;

	/// <summary>Practically invisible, but never culled as an empty mesh, so the graphic always takes clicks.</summary>
	public static void Style(Image image, Color debugColor) {
		image.color = On ? debugColor : new Color(0f, 0f, 0f, 0.004f);
		image.raycastTarget = true;
		var renderer = image.GetComponent<CanvasRenderer>();
		if (renderer != null) renderer.cullTransparentMesh = false;
	}

	public static readonly Color Spot = new Color(1f, 0.1f, 0.1f, 0.45f);
	public static readonly Color Catcher = new Color(0.1f, 1f, 0.2f, 0.12f);
}
