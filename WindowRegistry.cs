using System.Collections.Generic;

namespace DiceModders.UiKit;

internal static class WindowRegistry {
    private static readonly List<WindowHandle> Windows = new();
    public static int Count => Windows.Count;
    public static void Register(WindowHandle w)   { if (w != null && !Windows.Contains(w)) Windows.Add(w); }
    public static void Unregister(WindowHandle w) { Windows.Remove(w); }

	public static void BringToFront(WindowHandle w) {
        if (w?.Root == null) return;
        w.Root.SetAsLastSibling();
        Windows.Remove(w);
        Windows.Add(w);
    }
}
