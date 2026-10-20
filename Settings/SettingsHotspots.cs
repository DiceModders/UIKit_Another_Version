using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

/// <summary>Handle of a hidden click area; <see cref="Remove"/> deletes it.</summary>
public sealed class SettingsHotspot {
	internal SettingsHotspot(string id, SettingsCorner corner, int clicks, Action trigger, float size, float gap) {
		Id = id; Corner = corner; Clicks = Math.Max(1, clicks); Trigger = trigger; Size = size; MaxGap = gap;
	}
	public string Id { get; }
	internal SettingsCorner Corner { get; }
	internal int Clicks { get; }
	internal Action Trigger { get; }
	internal float Size { get; }
	internal float MaxGap { get; }
	internal int Count;
	internal float Last;
	internal readonly List<GameObject> Instances = new();

	internal void Click() {
		float now = Time.unscaledTime;
		if (now - Last > MaxGap) Count = 0;
		Last = now;
		if (++Count < Clicks) return;
		Count = 0;
		try { Trigger?.Invoke(); }
		catch (Exception e) { UiLog.Error($"hotspot '{Id}' failed: {e}"); }
	}

	public void Remove() => SettingsHotspots.Remove(this);
}

internal static class SettingsHotspots {
	private static readonly List<SettingsHotspot> Items = new();

	public static SettingsHotspot Add(string id, SettingsCorner corner, int clicks, Action trigger, float size, float gap) {
		if (string.IsNullOrEmpty(id)) throw new ArgumentException("A hotspot needs an id", nameof(id));
		foreach (var h in Items) if (h.Id == id) { UiLog.Warn($"hotspot '{id}' already exists - returning it"); return h; }
		var hotspot = new SettingsHotspot(id, corner, clicks, trigger, size, gap);
		Items.Add(hotspot);
		return hotspot;
	}

	public static void Remove(SettingsHotspot hotspot) {
		if (!Items.Remove(hotspot)) return;
		foreach (var go in hotspot.Instances) if (go != null) UnityEngine.Object.Destroy(go);
		hotspot.Instances.Clear();
	}

	/// <summary>Creates any missing hotspot objects under the Settings screen (called each time it is shown).</summary>
	public static void Ensure(RectTransform screen) {
		foreach (var h in Items) {
			string name = "DM Hotspot " + h.Id;
			if (screen.Find(name) != null) continue;
			Create(h, screen, name);
		}
	}

	private static void Create(SettingsHotspot h, RectTransform screen, string name) {
		var rt = UiFactory.NewRect(name, screen);
		bool left = h.Corner == SettingsCorner.TopLeft || h.Corner == SettingsCorner.BottomLeft;
		bool top = h.Corner == SettingsCorner.TopLeft || h.Corner == SettingsCorner.TopRight;
		var anchor = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
		rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
		rt.anchoredPosition = Vector2.zero;
		rt.sizeDelta = new Vector2(h.Size, h.Size);

		var image = rt.gameObject.AddComponent<Image>();
		image.color = new Color(0f, 0f, 0f, 0f);   // invisible, but it still receives clicks
		image.raycastTarget = true;
		var button = rt.gameObject.AddComponent<Button>();
		button.transition = Selectable.Transition.None;
		var action = Il2CppDelegates.ToUnityAction(() => h.Click());
		Il2CppDelegates.Hold(rt.gameObject, action);
		button.onClick.AddListener(action);
		h.Instances.Add(rt.gameObject);
	}
}
