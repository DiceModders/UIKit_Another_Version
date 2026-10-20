using System;

namespace DiceModders.UiKit;

/// <summary>
/// Entry point of the library. <c>UiKit.Window(...)</c> builds windows, <c>UiKit.TopBar</c> adds buttons next to the game's own top-right buttons,
/// <c>UiKit.Settings</c> adds rows to the game's Settings screens. Everything is made of clones of the game's own UI objects.
/// </summary>
public static partial class UiKit {
	private static int _nextId;

	/// <summary>
	/// Prepares the library (registers its helper components and the settings hook). Optional: every public entry point calls it,
	/// so a mod normally never needs to. Safe to call any number of times.
	/// </summary>
	public static void Init() => Core.Ensure();

	public static WindowHandle Window(Action<WindowHandle> build = null) {
		Core.Ensure();
		var canvas = UiRoot.Root;
		if (canvas == null) {
			UiLog.Error("UiKit.Window: no canvas available");
			throw new InvalidOperationException("No canvas available");
		}

		int id = ++_nextId;
		var handle = WindowHandle.Create(canvas.transform);
		try {
			build?.Invoke(handle);
		}
		catch (Exception e) {
			UiLog.Error($"Window #{id} build failed: {e}");
			handle.Close();
			throw;
		}
		handle.FinalizeBuild();
		return handle;
	}
}
