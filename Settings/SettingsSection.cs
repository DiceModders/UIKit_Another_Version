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
}
