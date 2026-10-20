using System;
using UnityEngine;
using GameIsland = DiceKingdoms.Game.Island;

namespace DiceModders.UiKit;

/// <summary>A column added to the match scoreboard. <see cref="Remove"/> takes it out again.</summary>
public sealed class ScoreboardColumn {
	internal ScoreboardColumn(string id, Func<GameIsland, string> text, string tooltip, Sprite icon) { Id = id; Text = text; Tooltip = tooltip; Icon = icon; }
	public string Id { get; }
	internal Func<GameIsland, string> Text { get; }
	internal string Tooltip { get; }
	internal Sprite Icon { get; }
	public void Remove() => ScoreboardColumns.Remove(this);
}

public static partial class UiKit {
	/// <summary>Extension points of the in-match scoreboard.</summary>
	public static class Scoreboard {
		/// <summary>
		/// Adds a column to every row of the scoreboard (one cell per kingdom, built from a copy of the game's own stat cell, so it looks like the others).
		/// </summary>
		/// <param name="id">Unique name of the column (your mod's name + a short word).</param>
		/// <param name="text">Gets the kingdom (<c>Island</c>) of a row and returns its cell text; called a few times per second, so keep it cheap.</param>
		/// <param name="tooltip">Optional hover text of the cell.</param>
		/// <param name="icon">Optional icon shown in the cell instead of the donor cell's icon.</param>
		public static ScoreboardColumn AddColumn(string id, Func<GameIsland, string> text, string tooltip = null, Sprite icon = null) {
			Core.Ensure();
			if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required", nameof(id));
			if (text == null) throw new ArgumentNullException(nameof(text));
			return ScoreboardColumns.Add(id, text, tooltip, icon);
		}
	}
}
