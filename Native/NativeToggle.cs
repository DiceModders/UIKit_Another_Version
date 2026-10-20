using System;
using UnityEngine;
using GameToggle = DiceKingdoms.Interface.Toggle;
using SettingInterfaceBool = DiceKingdoms.Meta.SettingInterfaceBool;

namespace DiceModders.UiKit;

/// <summary>
/// A checkbox that is a clone of the game's own boolean setting control (its <c>Toggle</c> with the checkbox art, the click sound and the checkmark).
/// The setting binding of the donor is removed before the clone wakes up, so nothing writes to the game's settings.
/// </summary>
internal sealed class NativeToggle {
	public GameObject Go { get; private set; }
	public GameToggle Game { get; private set; }
	public bool IsAlive => Go != null && Game != null;

	private bool _silent;
	private Action<bool> _changed;

	private NativeToggle() { }

	public bool Value => Game != null && Game.Value;

	/// <param name="prefab">The game's SettingInterfaceBool (prefab or live object) - null to search for one.</param>
	public static NativeToggle Create(SettingInterfaceBool prefab, Transform parent, bool initial, Action<bool> changed) {
		prefab ??= NativeTemplates.BoolSetting();
		if (prefab == null) return null;

		var go = UnityEngine.Object.Instantiate(prefab.gameObject, Staging.Root);   // parked: the clone's Awake has not run yet
		go.name = "DM Toggle";
		var binding = go.GetComponent<SettingInterfaceBool>();
		if (binding != null) UnityEngine.Object.DestroyImmediate(binding);          // no binding to a game setting
		var game = go.GetComponentInChildren<GameToggle>(true);
		if (game == null) { UnityEngine.Object.Destroy(go); return null; }

		go.transform.SetParent(parent, false);   // wakes the clone: the game's Toggle wires its own sound + change event now
		go.SetActive(true);

		var nt = new NativeToggle { Go = go, Game = game, _changed = changed };
		Action<bool> handler = nt.OnChange;
		var native = Il2CppDelegates.ToIl2CppActionBool(handler);
		game.onChange = native;                  // the game's own change event (also fires for programmatic changes: see _silent)
		Il2CppDelegates.Hold(go, new object[] { handler, native });
		nt.SetSilently(initial);
		return nt;
	}

	private void OnChange(bool value) {
		if (_silent) return;
		try { _changed?.Invoke(value); }
		catch (Exception e) { UiLog.Error("toggle: " + e); }
		NativeButton.Deselect();
	}

	/// <summary>Changes the checkbox without calling the change callback.</summary>
	public void SetSilently(bool value) {
		if (Game == null) return;
		_silent = true;
		try { Game.Value = value; }
		finally { _silent = false; }
	}

	public void SetInteractable(bool on) { if (Game != null) Game.Interactable = on; }

	public void Destroy() {
		if (Go != null) UnityEngine.Object.Destroy(Go);
		Go = null;
	}
}
