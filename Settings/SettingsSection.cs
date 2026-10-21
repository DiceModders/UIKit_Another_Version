using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameSetInterface = DiceKingdoms.Meta.SettingSetInterface;

namespace DiceModders.UiKit;

/// <summary>Adds rows to one block of a settings tab. Every row is a clone of the game's own settings row.</summary>
public sealed class SettingsSection {
	private readonly GameSetInterface _set;
	private readonly string _owner;
	private int _index;

	internal SettingsSection(GameSetInterface set, string owner) { _set = set; _owner = owner; }

	private string NextName(string kind) => $"DM Settings {_owner} {kind} {++_index}";

	/// <summary>A sub header (the same style as the game's own group headers).</summary>
	public SettingsSection Header(string text) {
		SettingsBuilder.AddHeader(_set, NextName("Header"), text);
		return this;
	}

	/// <summary>A checkbox row. <paramref name="get"/> is read now and again while the screen is open; <paramref name="set"/> is called when the player clicks.</summary>
	public SettingsSection Toggle(string label, Func<bool> get, Action<bool> set) {
		SettingsBuilder.AddToggle(_set, NextName("Toggle"), label, get, set);
		return this;
	}

	/// <summary>A row with a label and one button.</summary>
	public SettingsSection Button(string label, string buttonText, Action onClick) {
		SettingsBuilder.AddButtons(_set, NextName("Button"), label, new[] { (buttonText, onClick) });
		return this;
	}

	/// <summary>A row with a label and several buttons.</summary>
	public SettingsSection Buttons(string label, params (string text, Action onClick)[] buttons) {
		SettingsBuilder.AddButtons(_set, NextName("Buttons"), label, buttons);
		return this;
	}

	/// <summary>A row with "-" and "+" buttons around a value text. <paramref name="step"/> receives -1 or +1.</summary>
	public SettingsSection Stepper(string label, Func<string> valueText, Action<int> step) {
		SettingsBuilder.AddStepper(_set, NextName("Stepper"), label, valueText, step);
		return this;
	}

	/// <summary>A wrapped paragraph of plain text.</summary>
	public SettingsSection Info(string text) {
		SettingsBuilder.AddInfo(_set, NextName("Info"), text);
		return this;
	}

	// ---- rows that can be disabled or hidden while the screen is open

	/// <summary>A sub header that is shown only while <see cref="SettingsRowOptions.Visible"/> returns true.</summary>
	public SettingsSection Header(string text, SettingsRowOptions options) {
		Bind(SettingsBuilder.AddHeader(_set, NextName("Header"), text), options);
		return this;
	}

	/// <summary>A checkbox row that can be disabled (greyed out and not clickable) or hidden by <paramref name="options"/>; both are re-read four times a second.</summary>
	public SettingsSection Toggle(string label, Func<bool> get, Action<bool> set, SettingsRowOptions options) {
		Bind(SettingsBuilder.AddToggle(_set, NextName("Toggle"), label, get, set, options?.Enabled), options);
		return this;
	}

	/// <summary>A "-" / value / "+" row that can be disabled or hidden by <paramref name="options"/>.</summary>
	public SettingsSection Stepper(string label, Func<string> valueText, Action<int> step, SettingsRowOptions options) {
		Bind(SettingsBuilder.AddStepper(_set, NextName("Stepper"), label, valueText, step, options?.Enabled), options);
		return this;
	}

	/// <summary>A wrapped paragraph that can be hidden by <paramref name="options"/> (<see cref="SettingsRowOptions.Enabled"/> does not apply to text).</summary>
	public SettingsSection Info(string text, SettingsRowOptions options) {
		Bind(SettingsBuilder.AddInfo(_set, NextName("Info"), text), options);
		return this;
	}

	/// <summary>A wrapped paragraph whose text is re-read four times a second; the row is hidden while the text is empty and grows or shrinks with it.</summary>
	public SettingsSection Info(Func<string> text) {
		SettingsBuilder.AddLiveInfo(_set, NextName("Info"), text);
		return this;
	}

	private static void Bind(GameObject row, SettingsRowOptions options) {
		if (row == null || options?.Visible == null) return;
		var visible = options.Visible;
		bool Now() { try { return visible(); } catch (Exception e) { UiLog.Once("settings:visible", "settings row visible(): " + e.Message); return true; } }
		row.SetActive(Now());
		SettingsLive.Add(row, () => { bool v = Now(); if (row.activeSelf != v) row.SetActive(v); });
	}
}

/// <summary>Options of a settings row: both callbacks are re-read four times a second while the screen is open.</summary>
public sealed class SettingsRowOptions {
	/// <summary>Toggle and stepper rows: false greys the control out and makes it unclickable.</summary>
	public Func<bool> Enabled { get; set; }
	/// <summary>False hides the whole row (the rows below move up).</summary>
	public Func<bool> Visible { get; set; }
}
