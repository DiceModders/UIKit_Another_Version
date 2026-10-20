using System;
using Il2CppInterop.Runtime.Injection;

namespace DiceModders.UiKit;

/// <summary>One-time setup shared by every part of the library.</summary>
internal static class Core {
	private static bool _done;

	public static void Ensure() {
		if (_done) return;
		_done = true;
		Register<WindowDragHandler>();
		UiKitRunner.Spawn();
		SettingsHook.Install();
		UiLog.Info("native UI kit ready");
	}

	private static void Register<T>() where T : class {
		try { ClassInjector.RegisterTypeInIl2Cpp<T>(); }
		catch (Exception e) { UiLog.Warn($"could not register {typeof(T).Name}: {e.Message}"); }
	}
}
