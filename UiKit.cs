using System;

namespace DiceModders.UiKit;

public static class UiKit {
	private static int _nextId;

    public static WindowHandle Window(Action<WindowHandle> build = null) {
        var canvas = UiRoot.Root;
        if (canvas == null) {
            Plugin.Log.LogError("UiKit.Window: no canvas available");
            throw new InvalidOperationException("No canvas available");
        }

        int id = ++_nextId;
        var handle = WindowHandle.Create(canvas.transform);
        try {
            build?.Invoke(handle);
        }
        catch (Exception e) {
            Plugin.Log.LogError($"Window #{id} build failed: {e}");
            handle.Close();
            throw;
        }
        handle.FinalizeBuild();
        return handle;
    }
}
