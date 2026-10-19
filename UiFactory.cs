using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace DiceModders.UiKit;

public static class UiFactory {
    private static readonly Il2CppReferenceArray<Il2CppSystem.Type> RtTypes = BuildRtTypes();

	private static Il2CppReferenceArray<Il2CppSystem.Type> BuildRtTypes() {
		var types = new Il2CppReferenceArray<Il2CppSystem.Type>(1);
		types[0] = Il2CppType.Of<RectTransform>();
		return types;
	}

	public static RectTransform NewRect(string name, Transform parent) {
		var go = new GameObject(name, RtTypes);
		var rt = go.GetComponent<RectTransform>();

		rt.SetParent(parent, false);
		go.layer = parent.gameObject.layer;
		return rt;
	}

	public static TextMeshProUGUI MakeText(RectTransform parent, string name, string text, float size, TextAlignmentOptions align) {
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

	public static void Stretch(RectTransform rt) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
}
