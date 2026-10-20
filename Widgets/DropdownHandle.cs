using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameDropdown = DiceKingdoms.Interface.Dropdown;

namespace DiceModders.UiKit;

/// <summary>
/// A drop-down list. It is a clone of the game's own dropdown (the one of the Language setting): same art, the same open/close animation and list.
/// </summary>
public sealed class DropdownHandle : ElementHandle<DropdownHandle> {
	public TMP_Dropdown Dropdown { get; }

	private readonly Action<int> _changed;
	private readonly ButtonHandle _fallback;       // used only when the game's dropdown could not be cloned: a button that cycles through the options
	private List<string> _options;
	private int _index;
	private bool _silent;

	internal DropdownHandle(RectTransform root, TMP_Dropdown dropdown, IEnumerable<string> options, int index, Action<int> changed) : base(root) {
		Dropdown = dropdown;
		_changed = changed;
		_options = new List<string>(options ?? Array.Empty<string>());
		_index = Mathf.Clamp(index, 0, Math.Max(0, _options.Count - 1));

		Action<int> handler = OnPicked;
		var native = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<int>>(handler);
		dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
		dropdown.onValueChanged.AddListener(native);
		Il2CppDelegates.Hold(root.gameObject, new object[] { handler, native });
		Rebuild();
	}

	internal DropdownHandle(RectTransform root, ButtonHandle fallback, IEnumerable<string> options, int index, Action<int> changed) : base(root) {
		_fallback = fallback;
		_changed = changed;
		_options = new List<string>(options ?? Array.Empty<string>());
		_index = Mathf.Clamp(index, 0, Math.Max(0, _options.Count - 1));
		fallback.OnClick(() => {
			if (_options.Count == 0) return;
			_index = (_index + 1) % _options.Count;
			Rebuild();
			try { _changed?.Invoke(_index); } catch (Exception e) { UiLog.Error("dropdown: " + e); }
		});
		Rebuild();
	}

	public int Index => _index;
	public string Selected => _index >= 0 && _index < _options.Count ? _options[_index] : null;
	public int Count => _options.Count;

	/// <summary>Selects an entry without calling the change callback.</summary>
	public DropdownHandle SetIndex(int index) {
		_index = Mathf.Clamp(index, 0, Math.Max(0, _options.Count - 1));
		Rebuild();
		return this;
	}

	/// <summary>Replaces the entries (the selection is kept when it still exists).</summary>
	public DropdownHandle SetOptions(IEnumerable<string> options, int index = -1) {
		_options = new List<string>(options ?? Array.Empty<string>());
		_index = Mathf.Clamp(index >= 0 ? index : _index, 0, Math.Max(0, _options.Count - 1));
		Rebuild();
		return this;
	}

	public DropdownHandle SetInteractable(bool on) {
		if (Dropdown != null) Dropdown.interactable = on;
		_fallback?.SetInteractable(on);
		return this;
	}

	private void OnPicked(int index) {
		if (_silent) return;
		_index = index;
		try { _changed?.Invoke(index); }
		catch (Exception e) { UiLog.Error("dropdown: " + e); }
		NativeButton.Deselect();
	}

	private void Rebuild() {
		if (_fallback != null) {
			_fallback.SetText(Selected != null ? Selected + "  v" : "-");
			return;
		}
		if (Dropdown == null) return;
		_silent = true;
		try {
			Dropdown.options.Clear();
			foreach (var o in _options) Dropdown.options.Add(new TMP_Dropdown.OptionData(o));
			Dropdown.SetValueWithoutNotify(_index);
			Dropdown.RefreshShownValue();
		}
		finally { _silent = false; }
	}

	// ------------------------------------------------------------------ creation

	internal static DropdownHandle Create(RectTransform parent, IEnumerable<string> options, int index, Action<int> changed, Func<string, Action, ButtonHandle> makeFallbackButton) {
		var template = NativeTemplates.Dropdown();
		if (template != null) {
			try {
				var go = UnityEngine.Object.Instantiate(template.gameObject, Staging.Root);   // parked: the game's Awake has not run
				go.name = "DM Dropdown";
				NativeButton.StripForeign(go);
				var wrapper = go.GetComponent<GameDropdown>();
				if (wrapper != null) UnityEngine.Object.DestroyImmediate(wrapper);   // the TMP_Dropdown works on its own; the wrapper would feed the game's value list
				var tmp = go.GetComponent<TMP_Dropdown>();
				if (tmp != null) {
					go.transform.SetParent(parent, false);
					go.SetActive(true);
					var rt = go.GetComponent<RectTransform>();
					var handle = new DropdownHandle(rt, tmp, options, index, changed);
					handle.Layout.minWidth = handle.Layout.preferredWidth = 200f;
					handle.Layout.minHeight = handle.Layout.preferredHeight = Mathf.Clamp(rt.rect.height, 28f, 44f);
					handle.Layout.flexibleWidth = 0f;
					return handle;
				}
				UnityEngine.Object.Destroy(go);
			}
			catch (Exception e) { UiLog.Warn("native dropdown failed: " + e.Message); }
		}
		UiLog.Once("legacy-dropdown", "native dropdown template not found - using a button that cycles through the entries.");
		var button = makeFallbackButton("", null);
		return new DropdownHandle(button.Root, button, options, index, changed);
	}
}
