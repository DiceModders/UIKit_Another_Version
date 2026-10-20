using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

/// <summary>
/// A button in the game's top-right button bar (next to Scoreboard, Rules, Settings, Back to menu). It is a clone of the game's Rules button, so it
/// has the same size, shadow, hover/pressed art, cursor, sounds and tooltip as the buttons around it.
/// </summary>
public sealed class TopBarButton {
	internal TopBarButton(string id, int order, int sequence, Sprite icon, string tooltip, Action click) {
		Id = id;
		Order = order;
		Sequence = sequence;
		IconSprite = icon;
		Tip = tooltip;
		Click = click;
	}

	public string Id { get; }
	internal int Order { get; }
	internal int Sequence { get; }
	internal Sprite IconSprite;
	internal string Tip;
	internal Action Click;
	internal bool Visible = true;
	internal bool Interactable = true;
	internal NativeButton Native;

	/// <summary>False while the bar does not exist (e.g. between scenes); the button is re-created automatically.</summary>
	public bool IsAttached => Native != null && Native.IsAlive;

	/// <summary>The button's RectTransform (null while not attached).</summary>
	public RectTransform Root => IsAttached ? Native.Rt : null;

	/// <summary>Sets the icon by the name of one of the game's sprites (see <see cref="Assets.SpriteNames"/>).</summary>
	public TopBarButton SetIcon(string spriteName) {
		var sprite = Assets.GetSprite(spriteName);
		if (sprite == null) UiLog.Warn($"top bar button '{Id}': no game sprite named '{spriteName}'");
		return SetIcon(sprite);
	}

	public TopBarButton SetIcon(Sprite sprite) {
		if (sprite == null) return this;
		IconSprite = sprite;
		if (IsAttached) Native.SetIcon(sprite);
		return this;
	}

	public TopBarButton SetTooltip(string text) {
		bool needsRebuild = IsAttached && Native.Tip == null && !string.IsNullOrWhiteSpace(text);
		Tip = text;
		if (needsRebuild) { Native.Destroy(); Native = null; }   // re-created with a tooltip on the next tick
		else if (IsAttached) Native.SetTooltip(text);
		return this;
	}

	public TopBarButton OnClick(Action onClick) { Click = onClick; return this; }

	public TopBarButton SetVisible(bool visible) {
		Visible = visible;
		if (IsAttached) Native.SetVisible(visible);
		return this;
	}

	public TopBarButton SetInteractable(bool interactable) {
		Interactable = interactable;
		if (IsAttached) Native.SetInteractable(interactable);
		return this;
	}

	public void Remove() => UiKit.TopBar.Remove(this);
}

public static partial class UiKit {
	/// <summary>
	/// Registry of mod buttons in the game's top-right bar. Any number of mods can register buttons; each keeps its own id, and the buttons are placed
	/// left of the game's own buttons, sorted by <c>order</c> and then by registration, so mods never push each other around unpredictably.
	/// </summary>
	public static class TopBar {
		private static readonly List<TopBarButton> Entries = new();
		private static int _sequence;
		private static float _nextTick;

		/// <summary>Adds a button. <paramref name="icon"/> is the name of one of the game's sprites (see <see cref="Assets.SpriteNames"/>).</summary>
		public static TopBarButton Add(string id, string icon, Action onClick, string tooltip = null, int order = 0) {
			var sprite = string.IsNullOrEmpty(icon) ? null : Assets.GetSprite(icon);
			if (sprite == null && !string.IsNullOrEmpty(icon)) UiLog.Warn($"top bar button '{id}': no game sprite named '{icon}' - using a default icon");
			return Add(id, sprite, onClick, tooltip, order);
		}

		public static TopBarButton Add(string id, Sprite icon, Action onClick, string tooltip = null, int order = 0) {
			Core.Ensure();
			if (string.IsNullOrEmpty(id)) throw new ArgumentException("A top bar button needs an id", nameof(id));
			var existing = Get(id);
			if (existing != null) {
				UiLog.Warn($"top bar button '{id}' already exists - returning the existing button");
				return existing;
			}
			var button = new TopBarButton(id, order, ++_sequence, icon, tooltip, onClick);
			Entries.Add(button);
			_nextTick = 0f;   // attach on the next runner tick
			return button;
		}

		public static TopBarButton Get(string id) {
			foreach (var e in Entries) if (e.Id == id) return e;
			return null;
		}

		internal static void Remove(TopBarButton button) {
			if (button == null || !Entries.Remove(button)) return;
			button.Native?.Destroy();
			button.Native = null;
		}

		/// <summary>Called by the runner four times a second; does nothing while every button is attached.</summary>
		internal static void Tick(float now) {
			if (Entries.Count == 0 || now < _nextTick) return;

			bool missing = false;
			foreach (var e in Entries) if (!e.IsAttached) { missing = true; break; }
			if (!missing) { _nextTick = now + 1f; return; }

			_nextTick = now + 0.5f;
			var bar = GameUi.FindActiveBar();
			if (bar == null) return;   // no scene with a bar yet

			bool changed = false;
			foreach (var e in Entries) {
				if (e.IsAttached) continue;
				Attach(e, bar);
				changed |= e.IsAttached;
			}
			if (changed) Reorder(bar);
		}

		private static void Attach(TopBarButton button, Transform bar) {
			var captured = button;
			var native = NativeButton.CreateIcon(bar, "DM Bar " + button.Id, button.IconSprite ?? Assets.GetSprite("hammer"), () => captured.Click?.Invoke(), button.Tip);
			if (native == null) return;   // the template button does not exist yet; retried on the next tick
			button.Native = native;
			native.SetVisible(button.Visible);
			native.SetInteractable(button.Interactable);
			UiLog.Info($"top bar button '{button.Id}' added to '{bar.name}'");
		}

		/// <summary>Our buttons sit at the left end of the bar, sorted by order then registration.</summary>
		private static void Reorder(Transform bar) {
			var attached = new List<TopBarButton>();
			foreach (var e in Entries) if (e.IsAttached) attached.Add(e);
			attached.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));
			for (int i = 0; i < attached.Count; i++) attached[i].Native.Rt.SetSiblingIndex(i);
			var rect = bar.GetComponent<RectTransform>();
			if (rect != null) LayoutRebuilder.MarkLayoutForRebuild(rect);
		}
	}
}
