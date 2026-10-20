using System;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameButton = DiceKingdoms.Interface.BaseButton;
using GameButtonIcon = DiceKingdoms.Interface.ButtonIcon;
using GameButtonLabel = DiceKingdoms.Interface.ButtonLabel;
using GameToolTip = DiceKingdoms.Interface.HasToolTip;

namespace DiceModders.UiKit;

/// <summary>
/// A button that is a real clone of one of the game's own buttons. Because the clone keeps the game's <c>BaseButton</c> component it gets, for free and
/// exactly like the originals: hover/pressed/disabled art, the cursor change, the hover and click sounds, the drop shadow and the label tint.
/// <list type="bullet">
/// <item>Clicks use the game's own event, <c>BaseButton.onClick</c> (the game forwards the Unity click to it) - no EventSystem hacks or click scanning.</item>
/// <item>The donor's persistent Unity listeners are dropped (otherwise a cloned Rules button would still open the Rules screen).</item>
/// <item>The sprite of a clone is never cleared: <c>BaseButton.Update</c> maps the shown sprite's texture to a state and throws every frame for unknown textures.</item>
/// </list>
/// </summary>
internal sealed class NativeButton {
	/// <summary>The game's buttons are 32 units high.</summary>
	public const float Height = 32f;

	/// <summary>Sliced Button-1 art has 16-unit borders. At exactly 32 units the centre slice is zero and rounding leaves hairlines, so stay a hair below.</summary>
	public const float SlicedFit = 31.75f;

	public GameObject Go { get; private set; }
	public RectTransform Rt { get; private set; }
	public Image Face { get; private set; }
	public Button UnityButton { get; private set; }
	public GameButton Game { get; private set; }
	public TMP_Text Text { get; private set; }
	public GameToolTip Tip { get; private set; }

	public bool Selected { get; private set; } = true;

	/// <summary>Edge length (in units) the donor icon button was designed for; used to scale the icon when a clone is made smaller or larger.</summary>
	private float _designSize = 0f;
	private Vector2 _iconBox;
	private float _iconScale = 1f;
	private const float IconFill = 0.82f;

	private Image _dim;
	private GameButton.State _state = GameButton.State.READY;
	private bool _stateEvents;
	private Action _click;
	private bool _clickWired;

	private NativeButton() { }

	public bool IsAlive => Go != null;

	// ------------------------------------------------------------------ creation

	/// <summary>Text button, cloned from the Settings "Close" button.</summary>
	public static NativeButton CreateLabel(Transform parent, string name, string text, float fontSize, Action click) {
		var template = NativeTemplates.LabelButton();
		if (template == null) return null;
		var nb = Clone(template, parent, name, null);
		if (nb == null) return null;

		var label = nb.Go.GetComponent<GameButtonLabel>();
		nb.Text = label != null && label.label != null ? label.label.visual : null;
		if (nb.Text == null) nb.Text = nb.Go.GetComponentInChildren<TMP_Text>(true);
		if (nb.Text != null) {
			NativeText.LearnFrom(nb.Text);
			nb.Text.text = text ?? string.Empty;
			NativeText.FitInRect(nb.Text, fontSize);
		}
		nb.SetClick(click);
		return nb;
	}

	/// <summary>Icon button, cloned from the Rules "?" button of the top-right bar.</summary>
	public static NativeButton CreateIcon(Transform parent, string name, Sprite icon, Action click, string tooltip) {
		var template = NativeTemplates.IconButton();
		if (template == null) return null;
		var nb = Clone(template, parent, name, tooltip);
		if (nb == null) return null;
		var templateRect = template.GetComponent<RectTransform>();
		if (templateRect != null) nb._designSize = Mathf.Min(templateRect.rect.width, templateRect.rect.height);
		var donorImage = FindIconImageIn(template);
		if (donorImage != null) { var size = donorImage.rectTransform.rect.size; float edge = Mathf.Min(size.x, size.y); nb._iconBox = new Vector2(edge, edge); }
		else DescribeIconTemplate(template);
		nb.SetIcon(icon);
		nb.SetClick(click);
		return nb;
	}

	/// <summary>The small X button of the game's windows (About, Store ...), cloned with its own icon.</summary>
	public static NativeButton CreateClose(Transform parent, string name, Action click) {
		var template = NativeTemplates.CloseIconButton();
		if (template == null) return null;
		var nb = Clone(template, parent, name, null);
		if (nb == null) return null;
		nb.SetClick(click);
		return nb;
	}

	private static NativeButton Clone(GameObject template, Transform parent, string name, string tooltip) {
		// Parked under an inactive object: the clone's scripts do not run until we move it into the real UI below.
		var go = UnityEngine.Object.Instantiate(template, Staging.Root);
		go.name = name;
		StripForeign(go);

		var unity = go.GetComponent<Button>();
		if (unity == null) {
			UnityEngine.Object.Destroy(go);
			return null;
		}
		unity.onClick = new Button.ButtonClickedEvent();   // drops the donor's persistent listeners
		unity.interactable = true;

		// Tooltip: the game's own HasToolTip. A component without text would throw on hover, so it is either configured or removed.
		var tip = go.GetComponent<GameToolTip>();
		if (string.IsNullOrWhiteSpace(tooltip)) {
			if (tip != null) { UnityEngine.Object.DestroyImmediate(tip); tip = null; }
		}
		else {
			if (tip == null) tip = go.AddComponent<GameToolTip>();
			tip.localizedInitialText = new UnityEngine.Localization.LocalizedString();   // stops the donor's localized text from overriding ours
			tip.initialText = tooltip;
		}

		go.transform.SetParent(parent, false);   // activates the clone: the game's Awake/Start/OnEnable run now
		go.SetActive(true);

		var nb = new NativeButton {
			Go = go,
			Rt = go.GetComponent<RectTransform>(),
			Face = go.GetComponent<Image>(),
			UnityButton = unity,
			Game = go.GetComponent<GameButton>(),
			Tip = tip,
		};
		if (nb.Game == null) UiLog.Once("nobase:" + template.name, $"'{template.name}' has no BaseButton - cursor/sounds of clones will be missing.");
		if (tip != null) tip.UpdateInitialText(tooltip);
		return nb;
	}

	/// <summary>Removes leftovers of earlier changes of ours on the donor and anything that would re-localize or re-label the clone.</summary>
	internal static void StripForeign(GameObject go) {
		var t = go.transform;
		for (int i = t.childCount - 1; i >= 0; i--) {
			var c = t.GetChild(i);
			if (c != null && c.name.StartsWith("DM")) UnityEngine.Object.DestroyImmediate(c.gameObject);
		}
		try {
			foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) {
				if (mb == null) continue;
				string description = mb.ToString();
				if (description != null && description.Contains("Localize")) UnityEngine.Object.DestroyImmediate(mb);
			}
		}
		catch (Exception e) { UiLog.Warn("could not strip localization components: " + e.Message); }
	}

	// ------------------------------------------------------------------ content

	public void SetText(string value) {
		if (Text != null) Text.text = value ?? string.Empty;
	}

	public void SetIcon(Sprite sprite) {
		if (sprite == null || Go == null) return;
		var icon = Go.GetComponent<GameButtonIcon>();
		if (icon != null) icon.Icon = sprite;
		else { var image = FindIconImage(); if (image != null) image.sprite = sprite; }
		NormalizeIcon();
	}

	private Image FindIconImage() => FindIconImageIn(Go);

	/// <summary>The image that shows the icon: <c>Top/Icon</c>, else any child image named like an icon, else the largest non-background child image.</summary>
	private static Image FindIconImageIn(GameObject root) {
		if (root == null) return null;
		var t = root.transform.Find("Top/Icon");
		var image = t != null ? t.GetComponent<Image>() : null;
		if (image != null) return image;
		Image fallback = null;
		foreach (var i in root.GetComponentsInChildren<Image>(true)) {
			if (i == null || i.gameObject == root || Staging.IsOurs(i.transform)) continue;
			if (i.name.Contains("Icon")) return i;
			string n = i.name.ToLowerInvariant();
			if (n.Contains("shadow") || n.Contains("background") || n.Contains("outline") || n.Contains("border")) continue;
			if (fallback == null) fallback = i;
		}
		return fallback;
	}

	private static void DescribeIconTemplate(GameObject root) {
		var sb = new System.Text.StringBuilder("icon template '" + root.name + "' children:");
		foreach (var r in root.GetComponentsInChildren<RectTransform>(true)) {
			if (r == null || r.gameObject == root) continue;
			var img = r.GetComponent<Image>();
			sb.Append($"\n   {r.name}: rect={r.rect} anchors={r.anchorMin}-{r.anchorMax} pos={r.anchoredPosition} image={(img != null ? "yes" : "no")}");
		}
		UiLog.Once("icontemplate", sb.ToString());
	}

	/// <summary>
	/// Puts the icon in the same centred box the game's own icons use (taken from the donor), keeps its aspect ratio and makes it a little smaller than the box,
	/// because sprites with full-tile art look bigger than the glyphs the game draws with padding. <see cref="_iconScale"/> follows the button size.
	/// </summary>
	private void NormalizeIcon() {
		var image = FindIconImage();
		if (image == null || _iconBox.x < 1f) return;
		var rt = image.rectTransform;
		image.preserveAspect = true;
		rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
		rt.pivot = new Vector2(0.5f, 0.5f);
		rt.anchoredPosition = Vector2.zero;
		rt.localScale = Vector3.one;
		rt.sizeDelta = _iconBox * (IconFill * _iconScale);
	}

	/// <summary>Scales the icon with the button when the button is not the donor's size (e.g. 32 units inside a window).</summary>
	public void FitIcon(float buttonSize) {
		if (_designSize < 1f) return;
		_iconScale = Mathf.Clamp(buttonSize / _designSize, 0.25f, 4f);
		NormalizeIcon();
		UiLog.Once("icongeo", $"icon button: design={_designSize:0.#} iconBox={_iconBox} -> button {buttonSize:0.#} (scale {_iconScale:0.##})");
	}

	public void SetTooltip(string text) {
		if (Tip == null) { UiLog.Once("notip:" + (Go != null ? Go.name : "?"), "this button was created without a tooltip; pass one when creating it."); return; }
		if (!string.IsNullOrWhiteSpace(text)) Tip.UpdateInitialText(text);
	}

	public void SetVisible(bool visible) {
		if (Go != null && Go.activeSelf != visible) Go.SetActive(visible);
	}

	public void SetInteractable(bool on) {
		if (Go == null) return;
		if (Game != null) Game.Interactable = on;
		else UnityButton.interactable = on;
		UpdateDim();
	}

	// ------------------------------------------------------------------ click

	public void SetClick(Action click) {
		_click = click;
		if (_clickWired || Go == null) return;
		_clickWired = true;
		Action wrapped = OnClick;
		if (Game != null) {
			var native = Il2CppDelegates.ToIl2CppAction(wrapped);
			Game.onClick = native;                              // the game's own click event
			Il2CppDelegates.Hold(Go, new object[] { wrapped, native });
		}
		else {
			var native = Il2CppDelegates.ToUnityAction(wrapped);  // not a game button: plain Unity event
			UnityButton.onClick.AddListener(native);
			Il2CppDelegates.Hold(Go, new object[] { wrapped, native });
		}
	}

	private void OnClick() {
		try { _click?.Invoke(); }
		catch (Exception e) { UiLog.Error("button click: " + e); }
		Deselect();
	}

	/// <summary>Unity keeps a clicked button "selected" (its outline stays bright after the mouse leaves) until the selection is cleared.</summary>
	public static void Deselect() {
		try {
			var events = EventSystem.current;
			if (events != null) events.SetSelectedGameObject(null);
		}
		catch (Exception) { }
	}

	// ------------------------------------------------------------------ layout

	/// <summary>Sets the size for a layout group parent (preferred size) and for a plain parent (rect size).</summary>
	public void SetSize(float width, float height) {
		if (Go == null) return;
		width = Fit(width);
		height = Fit(height);
		var le = Go.GetComponent<LayoutElement>() ?? Go.AddComponent<LayoutElement>();
		le.minWidth = le.preferredWidth = width;
		le.minHeight = le.preferredHeight = height;
		Rt.sizeDelta = new Vector2(width, height);
		// below the 32-unit design size the 9-slice corners would overlap: shrink the borders with the button
		if (Face != null && height < 31f) Face.pixelsPerUnitMultiplier = 32f / Mathf.Max(8f, height);
	}

	public static float Fit(float v) => v > 31f && v < 33f ? SlicedFit : v;

	// ------------------------------------------------------------------ selected / unselected look

	/// <summary>
	/// Selected = the game's normal bright art; unselected = the game's dark "disabled" art (the same convention the Settings tabs use).
	/// The dark art is an overlay that is hidden while the mouse hovers or presses, so the native hover/pressed art still shows.
	/// </summary>
	public void SetSelected(bool selected) {
		Selected = selected;
		if (!selected) EnsureDim();
		UpdateDim();
	}

	private void EnsureDim() {
		if (_dim != null || Go == null) return;
		var dark = UnityButton.spriteState.disabledSprite;
		if (dark == null) return;

		var rt = UiFactory.NewRect("DM Dim", Rt);
		UiFactory.Stretch(rt);
		rt.SetAsFirstSibling();   // above the button's own image, below its label/icon
		_dim = rt.gameObject.AddComponent<Image>();
		_dim.sprite = dark;
		_dim.type = Face.type;
		_dim.pixelsPerUnitMultiplier = Face.pixelsPerUnitMultiplier;
		_dim.raycastTarget = false;

		if (Game == null) return;
		try {
			Action<GameButton.State, GameButton.State> changed = (previous, next) => { _state = next; UpdateDim(); };
			var native = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<GameButton.State, GameButton.State>>(changed);
			Game.onStateChange = Il2CppSystem.Delegate.Combine(Game.onStateChange, native)
				.Cast<Il2CppSystem.Action<GameButton.State, GameButton.State>>();
			Il2CppDelegates.Hold(Go, new object[] { changed, native });
			_stateEvents = true;
		}
		catch (Exception e) {
			UiLog.Warn("button state events unavailable (the dark look will also show on hover): " + e.Message);
		}
	}

	private void UpdateDim() {
		if (_dim == null) return;
		bool show = !Selected && UnityButton.interactable && (!_stateEvents || _state == GameButton.State.READY);
		if (_dim.gameObject.activeSelf != show) _dim.gameObject.SetActive(show);
	}

	public void Destroy() {
		if (Go != null) UnityEngine.Object.Destroy(Go);
		Go = null;
	}
}
