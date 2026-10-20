using System;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace DiceModders.UiKit;

/// <summary>
/// One tiny persistent MonoBehaviour that wakes the library four times a second. All the heavy lifting is event driven (the settings screens through
/// a hook on the game's own GenerateElements, clicks through the game's own events); this only re-attaches top-bar buttons after a scene change and
/// keeps live settings rows in sync. With nothing registered it returns after one float compare.
/// </summary>
internal sealed class UiKitRunner : MonoBehaviour {
	public UiKitRunner(IntPtr ptr) : base(ptr) { }

	private static UiKitRunner _instance;
	private static bool _registered;
	private float _next;
	private float _nextSweep;

	internal static void Spawn() {
		if (_instance != null) return;
		if (!_registered) {
			_registered = true;
			try { ClassInjector.RegisterTypeInIl2Cpp<UiKitRunner>(); }
			catch (Exception e) { UiLog.Warn("runner registration: " + e.Message); }
		}
		var go = new GameObject("DiceModders UiKit");
		go.hideFlags = HideFlags.HideAndDontSave;
		DontDestroyOnLoad(go);
		_instance = go.AddComponent<UiKitRunner>();
	}

	public void Update() {
		SettingsCloseShift.Frame();   // per frame, but returns at once unless a Settings tab with a scrollbar is open
		float now = Time.unscaledTime;
		if (now < _next) return;
		_next = now + 0.25f;
		try {
			UiKit.TopBar.Tick(now);
			SettingsLive.Tick(now);
			SettingsCloseShift.Tick();
			if (now >= _nextSweep) { _nextSweep = now + 10f; Il2CppDelegates.Sweep(); }
		}
		catch (Exception e) { UiLog.Once("runner:" + e.GetType().Name, "tick failed: " + e); }
	}
}
