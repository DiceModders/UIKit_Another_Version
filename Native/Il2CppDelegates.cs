using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using UnityEngine.Events;

namespace DiceModders.UiKit;

/// <summary>
/// Converts managed delegates to the game's IL2CPP delegate types and keeps them alive exactly as long as the Unity object they belong to.
/// (Plain <c>ConvertDelegate</c> results can be collected while the game still holds them.)
/// </summary>
internal static class Il2CppDelegates {
	private static readonly List<(UnityEngine.Object Owner, object Keep)> Alive = new();

	/// <summary>Keeps <paramref name="keep"/> alive while <paramref name="owner"/> exists.</summary>
	public static void Hold(UnityEngine.Object owner, object keep) {
		if (owner == null || keep == null) return;
		Alive.Add((owner, keep));
	}

	/// <summary>Drops references whose Unity owner was destroyed. Called a few times a minute by the runner.</summary>
	public static void Sweep() {
		if (Alive.Count > 0) Alive.RemoveAll(e => e.Owner == null);
	}

	public static Il2CppSystem.Action ToIl2CppAction(Action a)
		=> DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(a);

	public static Il2CppSystem.Action<bool> ToIl2CppActionBool(Action<bool> a)
		=> DelegateSupport.ConvertDelegate<Il2CppSystem.Action<bool>>(a);

	public static UnityAction ToUnityAction(Action a)
		=> DelegateSupport.ConvertDelegate<UnityAction>(a);

	public static UnityAction<bool> ToUnityActionBool(Action<bool> a)
		=> DelegateSupport.ConvertDelegate<UnityAction<bool>>(a);
}
