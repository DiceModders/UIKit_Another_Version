using System;

namespace DiceModders.UiKit;

public static class UiKit {
    public static WindowHandle Window(string title, Action<WindowHandle> build = null) {
        var canvas = UIRoot.Root;
        if (canvas == null) Plugin.Log.LogError("No canvas available");

        var handle = WindowHandle.Create(title, canvas.transform);
        try {
            build?.Invoke(handle);
        }
        catch (Exception e) {
            Plugin.Log.LogError($"Window '{title}' build failed: {e}");
            handle.Close();
            throw;
        }
        handle.FinalizeBuild();
        return handle;
    }
}
