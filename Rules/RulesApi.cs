using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceModders.UiKit;

/// <summary>An entry added to the civilization drop-down of the Rules screen. <see cref="Remove"/> takes it out again (it disappears the next time the screen is built).</summary>
public sealed class RulesCivilizationEntry {
	internal RulesCivilizationEntry(int civilizationId, string text) { CivilizationId = civilizationId; Text = text; }
	/// <summary>The value the game receives as <c>Civilization</c> when the entry is picked.</summary>
	public int CivilizationId { get; }
	/// <summary>The text shown in the drop-down.</summary>
	public string Text { get; }
	public void Remove() => RulesRegistry.Remove(this);
}

internal static class RulesRegistry {
	private static readonly List<RulesCivilizationEntry> Items = new();

	public static IReadOnlyList<RulesCivilizationEntry> All => Items;

	public static RulesCivilizationEntry Add(int civilizationId, string text) {
		Items.RemoveAll(e => e.CivilizationId == civilizationId);   // the same number again replaces the entry
		var entry = new RulesCivilizationEntry(civilizationId, text);
		Items.Add(entry);
		return entry;
	}

	public static void Remove(RulesCivilizationEntry entry) => Items.Remove(entry);
}

public static partial class UiKit {
	/// <summary>Extension points of the game's Rules screen.</summary>
	public static class Rules {
		/// <summary>
		/// Adds an entry to the civilization drop-down of the Rules screen. Choosing it makes the screen select that civilization number. This is only the
		/// drop-down entry: whatever the game needs to know about a civilization beyond the five it ships with (its tech tree, its skins) is not UiKit's
		/// business - a mod library that registers civilizations (ModCore) provides it, and the ModBridge plugin connects the two.
		/// </summary>
		/// <param name="civilizationId">The number the game receives as <c>Civilization</c> (the game's own enum has 0..4).</param>
		/// <param name="text">Text shown in the drop-down.</param>
		public static RulesCivilizationEntry AddCivilizationEntry(int civilizationId, string text) {
			Core.Ensure();
			if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("text is required", nameof(text));
			return RulesRegistry.Add(civilizationId, text);
		}

		/// <summary>Raised each time the Rules screen is shown. The argument is the screen's root RectTransform.</summary>
		public static event Action<RectTransform> Opened;
		internal static void RaiseOpened(RectTransform screen) {
			var handler = Opened;
			if (handler == null) return;
			foreach (Action<RectTransform> d in handler.GetInvocationList()) {
				try { d(screen); }
				catch (Exception e) { UiLog.Error("Rules.Opened handler failed: " + e); }
			}
		}
	}
}
