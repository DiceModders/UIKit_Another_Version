using System.Collections.Generic;

namespace DiceModders.UiKit;

/// <summary>Null-safe logging helper (the library may be used before <see cref="Plugin"/> has set its logger).</summary>
internal static class UiLog {
	private static readonly HashSet<string> Seen = new();

	public static void Info(string message)  => Plugin.Log?.LogInfo("[uikit] " + message);
	public static void Warn(string message)  => Plugin.Log?.LogWarning("[uikit] " + message);
	public static void Error(string message) => Plugin.Log?.LogError("[uikit] " + message);

	/// <summary>Logs a message only the first time a key is seen (for things that would otherwise repeat every frame).</summary>
	public static void Once(string key, string message) {
		if (Seen.Add(key)) Warn(message);
	}
}
