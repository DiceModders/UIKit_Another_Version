using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace DiceModders.UiKit;

public enum WindowState {
    Normal,
    Minimized,
    Fullscreen,
}

public sealed class WindowHandle {
    public const float DefaultWidth    = 400f;
    public const float DefaultHeight   = 260f;
    public const float TitleBarHeight  = 32f;
    public const float CloseButtonSize = 24f;
    public const float Padding         = 8f;
    public const float MinWidth        = 160f;
    public const float MinHeight       = 100f;

	public static string MinimizeGlyph = "–";
	public static string MaximizeGlyph = "□";
	public static string RestoreGlyph  = "❐";
	public static string CloseGlyph    = "×";

    public string Title { get; private set; }
    public RectTransform Root { get; private set; }
    public RectTransform TitleBar { get; private set; }
    public RectTransform BodyArea { get; private set; }
    public TextMeshProUGUI TitleText { get; private set; }
    public GameObject CloseButton { get; private set; }

    private ContainerHandle _body;
    private float _width  = DefaultWidth;
    private float _height = DefaultHeight;
    private Vector2 _position;
    private bool _positioned;
    private bool _draggable, _closable = true, _minimizable, _fullscreenable;
    private bool _closed;
	private WindowDragHandler _dragHandler;

	private WindowState _state = WindowState.Normal;
	private Vector2 _normalAnchorMin, _normalAnchorMax, _normalPivot;
	private Vector2 _normalAnchoredPosition, _normalSizeDelta;
	private bool _haveNormalSnapshot;

	private GameObject _minimizeButton, _maximizeButton;
	private TextMeshProUGUI _minimizeLabel, _maximizeLabel;

	public WindowState State => _state;
	public bool CanMinimize         => _minimizable;
	public bool CanToggleFullscreen => _fullscreenable;

    private WindowHandle() { }

    internal static WindowHandle Create(string title, Transform canvas) {
        var h = new WindowHandle { Title = title ?? "Window" };
        h.BuildVisual(canvas);
        return h;
    }

	private void BuildTitleBar() {
		TitleBar = NewRect("TitleBar", Root);
		TitleBar.anchorMin = new Vector2(0f, 1f);
		TitleBar.anchorMax = new Vector2(1f, 1f);
		TitleBar.pivot = new Vector2(0.5f, 1f);
		TitleBar.sizeDelta = new Vector2(0f, TitleBarHeight);
		TitleBar.anchoredPosition = Vector2.zero;
		TitleBar.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

		TitleText = MakeText(TitleBar, "Title", Title, 18f, TextAlignmentOptions.Left);
		TitleText.rectTransform.anchorMin = new Vector2(0f, 0f);
		TitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
		TitleText.rectTransform.offsetMin = new Vector2(Padding, 0f);

		CloseButton = MakeTitleButton("CloseBtn", CloseGlyph,
				new Color(0.55f, 0.18f, 0.18f, 1f), Close);

		(_maximizeButton, _maximizeLabel) = MakeTitleButtonWithLabel("MaxBtn", MaximizeGlyph,
				new Color(0.25f, 0.25f, 0.25f, 0.9f),
				() => { if (_state == WindowState.Fullscreen) Restore(); else Fullscreen(); });

		(_minimizeButton, _minimizeLabel) = MakeTitleButtonWithLabel("MinBtn", MinimizeGlyph,
				new Color(0.25f, 0.25f, 0.25f, 0.9f),
				() => { if (_state == WindowState.Minimized) Restore(); else Minimize(); });
	}

	private GameObject MakeTitleButton(string name, string glyph, Color bgColor, System.Action onClick) {
		var (go, _) = MakeTitleButtonWithLabel(name, glyph, bgColor, onClick);
		return go;
	}

	private (GameObject, TextMeshProUGUI) MakeTitleButtonWithLabel(string name, string glyph, Color bgColor, System.Action onClick) {
		var rt = NewRect(name, TitleBar);
		rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
		rt.pivot = new Vector2(1f, 0.5f);
		rt.sizeDelta = new Vector2(CloseButtonSize, CloseButtonSize);

		var img = rt.gameObject.AddComponent<Image>();
		img.color = bgColor;

		var btn = rt.gameObject.AddComponent<Button>();
		btn.targetGraphic = img;
		AttachClick(btn, onClick);

		var label = MakeText(rt, "Glyph", glyph, 14f, TextAlignmentOptions.Center);
		Stretch(label.rectTransform);

		return (rt.gameObject, label);
	}

    private void BuildVisual(Transform canvas) {
        Root = NewRect("Window", canvas);
        Root.anchorMin = Root.anchorMax = new Vector2(0f, 1f);
        Root.pivot = new Vector2(0f, 1f);

        var bg = Root.gameObject.AddComponent<Image>();
        var panelSprite = Assets.PanelBg();
        if (panelSprite != null) { bg.sprite = panelSprite; bg.type = Image.Type.Sliced; bg.color = Color.white; }
        else bg.color = new Color(0.12f, 0.10f, 0.08f, 0.98f);

		BuildTitleBar();

        BodyArea = NewRect("Body", Root);
        BodyArea.anchorMin = new Vector2(0f, 0f);
        BodyArea.anchorMax = new Vector2(1f, 1f);
        BodyArea.offsetMin = new Vector2(Padding, Padding);
        BodyArea.offsetMax = new Vector2(-Padding, -TitleBarHeight - Padding);

        var vlg = BodyArea.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        _body = new ContainerHandle(BodyArea);
        ApplyLayout();
    }

	public WindowHandle Size(float width, float height) {
		_width  = Mathf.Max(MinWidth, width);
		_height = Mathf.Max(MinHeight, height);
		if (_haveNormalSnapshot) _normalSizeDelta = new Vector2(_width, _height);
		if (_state == WindowState.Normal) ApplyLayout();
		return this;
	}

	public WindowHandle Position(float x, float y) {
		_position = new Vector2(x, y);
		_positioned = true;
		if (_haveNormalSnapshot) _normalAnchoredPosition = new Vector2(x, -y);
		if (_state == WindowState.Normal) ApplyLayout();
		return this;
	}

	public WindowHandle Draggable(bool on = true) {
		_draggable = on;
		ApplyDraggable();
		return this;
	}

	private void ApplyDraggable() {
		if (TitleBar == null) return;

		if (_draggable && _dragHandler == null) {
			_dragHandler = TitleBar.gameObject.AddComponent<WindowDragHandler>();
			_dragHandler.WindowRoot = Root;
			_dragHandler.Owner = this;
			_dragHandler.TitleBarHeight = TitleBarHeight;
		}
		else if (!_draggable && _dragHandler != null) {
			UnityEngine.Object.Destroy(_dragHandler);
			_dragHandler = null;
		}
	}

	public WindowHandle Closable(bool on = true)       { _closable = on;       ApplyFlags(); return this; }
	public WindowHandle Minimizable(bool on = true)    { _minimizable = on;    ApplyFlags(); return this; }
	public WindowHandle Fullscreenable(bool on = true) { _fullscreenable = on; ApplyFlags(); return this; }

    public WindowHandle SetTitle(string title) {
        Title = title ?? string.Empty;
        if (TitleText != null) TitleText.text = Title;
        return this;
    }

    public ContainerHandle Body() => _body;

    public bool IsOpen => !_closed && Root != null && Root.gameObject != null;

    public void Show()  { if (IsOpen) Root.gameObject.SetActive(true); }
    public void Hide()  { if (IsOpen) Root.gameObject.SetActive(false); }

    public void Close() {
        if (_closed) return;
        _closed = true;
        WindowRegistry.Unregister(this);
        if (Root != null && Root.gameObject != null)
            UnityEngine.Object.Destroy(Root.gameObject);
        Root = null;
        _body = null;
    }

	internal void FinalizeBuild() {
		if (!_positioned)
		{
			int n = WindowRegistry.Count;
			Position(60f + n * 28f, 60f + n * 28f);
		}
		ApplyDraggable();
		ApplyFlags();
		WindowRegistry.Register(this);
	}

    private void ApplyLayout() {
        if (Root == null) return;
        Root.sizeDelta = new Vector2(_width, _height);
        Root.anchoredPosition = new Vector2(_position.x, -_position.y);
    }

	internal static RectTransform NewRect(string name, Transform parent) {
		var types = new Il2CppReferenceArray<Il2CppSystem.Type>(1);
		types[0] = Il2CppType.Of<RectTransform>();

		var go = new GameObject(name, types);
		var rt = go.GetComponent<RectTransform>();

		rt.SetParent(parent, false);
		go.layer = parent.gameObject.layer;
		return rt;
	}

    internal static void Stretch(RectTransform rt) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    internal static TextMeshProUGUI MakeText(RectTransform parent, string name, string text, float size, TextAlignmentOptions align) {
        var rt = NewRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        var font = Assets.Font;
        if (font != null) tmp.font = font;
        var mat = Assets.FontShadowMaterial;
        if (mat != null) tmp.fontSharedMaterial = mat;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = new Color(0.969f, 0.940f, 0.893f, 1f);
        tmp.raycastTarget = false;
        return tmp;
    }

    internal static void AttachClick(Button btn, System.Action action) {
        if (btn == null || action == null) return;
        try {
            btn.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(action));
        }
        catch (System.Exception e) {
            Plugin.Log.LogError($"AttachClick failed: {e}");
        }
    }

	public void BringToFront() {
		if (!IsOpen) return;
		WindowRegistry.BringToFront(this);
	}

	public WindowHandle Minimize() {
		if (_minimizable) SetState(WindowState.Minimized);
		return this;
	}

	public WindowHandle Fullscreen() {
		if (_fullscreenable) SetState(WindowState.Fullscreen);
		return this;
	}

	public WindowHandle Restore() {
		SetState(WindowState.Normal);
		return this;
	}

	public WindowHandle SetState(WindowState state) {
		if (_state == state) return this;
		if (_state == WindowState.Normal) TakeSnapshot();
		_state = state;
		ApplyStateVisual();
		return this;
	}

	private void ApplyFlags() {
		if (CloseButton      != null) CloseButton.SetActive(_closable);
		if (_maximizeButton  != null) _maximizeButton.SetActive(_fullscreenable);
		if (_minimizeButton  != null) _minimizeButton.SetActive(_minimizable);

		// Расставляем видимые кнопки справа налево: close, max, min.
		float offset = -4f;
		if (_closable)      { PlaceButton(CloseButton,     offset); offset -= CloseButtonSize + 4f; }
		if (_fullscreenable){ PlaceButton(_maximizeButton, offset); offset -= CloseButtonSize + 4f; }
		if (_minimizable)   { PlaceButton(_minimizeButton, offset); offset -= CloseButtonSize + 4f; }

		float titleRight = Mathf.Max(Mathf.Abs(offset), Padding);
		if (TitleText != null)
			TitleText.rectTransform.offsetMax = new Vector2(-titleRight, 0f);
	}

	private static void PlaceButton(GameObject btn, float rightOffset) {
		if (btn == null) return;
		var rt = btn.GetComponent<RectTransform>();  // IL2CPP-тип, дженерик безопасен
		if (rt != null) rt.anchoredPosition = new Vector2(rightOffset, 0f);
	}

	private void TakeSnapshot() {
		_normalAnchorMin        = Root.anchorMin;
		_normalAnchorMax        = Root.anchorMax;
		_normalPivot            = Root.pivot;
		_normalAnchoredPosition = Root.anchoredPosition;
		_normalSizeDelta        = Root.sizeDelta;
		_haveNormalSnapshot     = true;
	}

	private void ApplyStateVisual() {
		if (Root == null) return;

		switch (_state) {
			case WindowState.Normal:
				if (_haveNormalSnapshot) {
					Root.anchorMin        = _normalAnchorMin;
					Root.anchorMax        = _normalAnchorMax;
					Root.pivot            = _normalPivot;
					Root.anchoredPosition = _normalAnchoredPosition;
					Root.sizeDelta        = _normalSizeDelta;
				}
				else {
					ApplyLayout();
				}
				if (BodyArea != null) BodyArea.gameObject.SetActive(true);
				break;

			case WindowState.Minimized:
				Root.anchorMin        = Root.anchorMax = new Vector2(0f, 1f);
				Root.pivot            = new Vector2(0f, 1f);
				Root.anchoredPosition = _haveNormalSnapshot
					? _normalAnchoredPosition
					: new Vector2(_position.x, -_position.y);
				Root.sizeDelta = new Vector2(_width, TitleBarHeight);
				if (BodyArea != null) BodyArea.gameObject.SetActive(false);
				break;

			case WindowState.Fullscreen:
				Root.anchorMin = Vector2.zero;
				Root.anchorMax = Vector2.one;
				Root.offsetMin = Vector2.zero;
				Root.offsetMax = Vector2.zero;
				if (BodyArea != null) BodyArea.gameObject.SetActive(true);
				break;
		}

		if (_dragHandler != null) _dragHandler.enabled = _state == WindowState.Normal;
		if (_maximizeLabel != null) _maximizeLabel.text = _state == WindowState.Fullscreen ? RestoreGlyph : MaximizeGlyph;
	}

	internal void ToggleFullscreenFromDoubleClick() {
		if (_state == WindowState.Fullscreen) Restore();
		else Fullscreen();
	}
}
