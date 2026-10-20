using UnityEngine;

namespace DiceModders.UiKit;

/// <summary>
/// An inactive, persistent parent for freshly cloned game objects. A clone parked here does not run Awake/OnEnable/Start,
/// so we can strip or reconfigure components before the game's own scripts see it, then move it into the real UI (which activates it).
/// </summary>
internal static class Staging {
	private static GameObject _go;

	public static Transform Root {
		get {
			if (_go == null) {
				_go = new GameObject("DM Staging");
				_go.SetActive(false);
				Object.DontDestroyOnLoad(_go);
			}
			return _go.transform;
		}
	}

	/// <summary>True for objects created by this library (so template searches never pick our own clones).</summary>
	public static bool IsOurs(Transform t) {
		for (; t != null; t = t.parent) {
			string n = t.name;
			if (n.StartsWith("DM ") || n.StartsWith("DiceModders")) return true;
		}
		return false;
	}
}
