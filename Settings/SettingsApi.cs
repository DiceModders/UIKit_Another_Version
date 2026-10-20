using System;
using System.Collections.Generic;

namespace DiceModders.UiKit;

/// <summary>The tabs of the game's Settings screen that mods can add rows to.</summary>
public enum SettingsTab { Video, Audio, Input, Game }

/// <summary>Handle of a registered settings block; <see cref="Remove"/> takes it out again (it disappears the next time the screen is built).</summary>
public sealed class SettingsRegistration {
	internal SettingsRegistration(SettingsTab tab, string title, Action<SettingsSection> build, int order, int sequence) {
		Tab = tab; Title = title; Build = build; Order = order; Sequence = sequence;
	}
	public SettingsTab Tab { get; }
	public string Title { get; }
	internal Action<SettingsSection> Build { get; }
	internal int Order { get; }
	internal int Sequence { get; }
	public void Remove() => SettingsRegistry.Remove(this);
}

internal static class SettingsRegistry {
	private static readonly List<SettingsRegistration> Items = new();
	private static int _sequence;

	public static SettingsRegistration Add(SettingsTab tab, string title, Action<SettingsSection> build, int order) {
		var reg = new SettingsRegistration(tab, title, build, order, ++_sequence);
		Items.Add(reg);
		return reg;
	}

	public static void Remove(SettingsRegistration reg) => Items.Remove(reg);

	public static List<SettingsRegistration> For(SettingsTab tab) {
		var list = new List<SettingsRegistration>();
		foreach (var i in Items) if (i.Tab == tab) list.Add(i);
		list.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));
		return list;
	}
}

public static partial class UiKit {
	/// <summary>
	/// Adds rows to the game's own Settings screen. The rows are built with the game's own row prefabs, so they look and lay out exactly like the
	/// built-in ones. <c>build</c> runs every time the game rebuilds that tab (each time the screen is opened), so it can simply read current values.
	/// </summary>
	public static class Settings {
		/// <param name="tab">Which tab gets the rows.</param>
		/// <param name="title">Header shown above the rows (usually your mod's name).</param>
		/// <param name="build">Adds the rows: <c>s.Toggle(...)</c>, <c>s.Button(...)</c>, <c>s.Stepper(...)</c>, <c>s.Info(...)</c>.</param>
		/// <param name="order">Lower values come first among mods.</param>
		public static SettingsRegistration Add(SettingsTab tab, string title, Action<SettingsSection> build, int order = 0) {
			Core.Ensure();
			if (build == null) throw new ArgumentNullException(nameof(build));
			return SettingsRegistry.Add(tab, title, build, order);
		}
	}
}
