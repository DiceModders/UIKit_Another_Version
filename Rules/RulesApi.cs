using System;
using System.Collections.Generic;
using UnityEngine;
using GameTechTree = DiceKingdoms.Game.TechTree;

namespace DiceModders.UiKit;

/// <summary>A civilization added to the game's Rules screen. <see cref="Remove"/> takes it out again (it disappears the next time the screen is built).</summary>
public sealed class RulesCivilization {
	internal string Key;
	internal RulesCivilization(int id, string name, Func<GameTechTree> techTree, int skinsLike) { Id = id; Name = name; TechTree = techTree; SkinsLike = skinsLike; }
	/// <summary>The value used as <c>Civilization</c> (the game's enum has 0..4; pick an unused number).</summary>
	public int Id { get; }
	public string Name { get; }
	internal Func<GameTechTree> TechTree { get; }
	/// <summary>The built-in civilization whose biome and visuals this one borrows (the game only knows how to skin 0..4).</summary>
	public int SkinsLike { get; }
	public void Remove() => RulesRegistry.Remove(this);
}

internal static class RulesRegistry {
	private static readonly List<RulesCivilization> Items = new();

	public static IReadOnlyList<RulesCivilization> All => Items;

	/// <summary>Stable id from a key: the same key gives the same id on every machine (needed for multiplayer); a taken id moves to the next free one.</summary>
	public static int AllocateId(string key) {
		uint h = 2166136261;
		foreach (char ch in key) { h ^= ch; h *= 16777619; }
		int id = 1000 + (int)(h % 1_000_000_000u);
		while (Items.Exists(c => c.Id == id && c.Key != key)) id++;
		return id;
	}

	public static RulesCivilization Add(int id, string name, Func<GameTechTree> techTree, int skinsLike) {
		var clash = Items.Find(c => c.Id == id);
		if (clash != null && clash.Name != name)
			UiLog.Warn($"Rules: civilization id {id} is already used by '{clash.Name}' - '{name}' replaces it. Use UiKit.Rules.AddCivilization(\"your.mod.civ\", ...) to get a free id automatically.");
		Items.RemoveAll(c => c.Id == id);   // registering the same id again replaces it
		var civ = new RulesCivilization(id, name, techTree, skinsLike);
		Items.Add(civ);
		return civ;
	}

	public static void Remove(RulesCivilization civ) => Items.Remove(civ);

	public static bool TryGetSkinsLike(int id, out int like) {
		foreach (var c in Items) if (c.Id == id) { like = c.SkinsLike; return true; }
		like = 0;
		return false;
	}

	public static bool TryGetTree(int id, out GameTechTree tree) {
		tree = null;
		foreach (var c in Items) {
			if (c.Id != id || c.TechTree == null) continue;
			try { tree = c.TechTree(); } catch (Exception e) { UiLog.Error($"tech tree provider of '{c.Name}': {e}"); }
			if (tree != null) return true;
		}
		return false;
	}
}

public static partial class UiKit {
	/// <summary>Extension points of the game's Rules screen.</summary>
	public static class Rules {
		/// <summary>
		/// Adds an entry to the civilization drop-down of the Rules screen. Choosing it selects that civilization there, so the Technologies tab shows its tech tree.
		/// </summary>
		/// <param name="id">The civilization number (the game's own use 0..4). The mod's own civilization data must use the same number.</param>
		/// <param name="name">Text shown in the drop-down.</param>
		/// <param name="techTree">
		/// Returns the <c>TechTree</c> of that civilization. The game looks trees up in its own list (and throws for an unknown civilization), so this is used
		/// whenever the game asks for the tree of <paramref name="id"/> - on the Rules screen and in the match. May be null if the mod adds the tree to the game's list itself.
		/// </param>
		/// <param name="skinsLike">
		/// The game sets a player's biome from the civilization with a switch over 0..4 and throws for anything else. For <paramref name="id"/> the biome and visuals
		/// of this built-in civilization (0 Europe, 1 Middle East, 2 Viking, 3 Ancient, 4 Maya) are used instead; the civilization itself stays <paramref name="id"/>.
		/// </param>
		public static RulesCivilization AddCivilization(int id, string name, Func<GameTechTree> techTree = null, int skinsLike = 0) {
			Core.Ensure();
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is required", nameof(name));
			return RulesRegistry.Add(id, name, techTree, skinsLike);
		}

		/// <summary>
		/// Same as the other overload, but UiKit picks the number so mods never collide: the key (e.g. "authorname.civname") becomes a fixed id
		/// (1000 and up), the same on every machine, so multiplayer and saves agree. Read it back from <see cref="RulesCivilization.Id"/> to tag your own data.
		/// </summary>
		public static RulesCivilization AddCivilization(string key, string name, Func<GameTechTree> techTree = null, int skinsLike = 0) {
			Core.Ensure();
			if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key is required", nameof(key));
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is required", nameof(name));
			var civ = RulesRegistry.Add(RulesRegistry.AllocateId(key), name, techTree, skinsLike);
			civ.Key = key;
			return civ;
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
