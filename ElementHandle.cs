using UnityEngine;
using UnityEngine.UI;

namespace DiceModders.UiKit;

public abstract class ElementHandle<T> where T : ElementHandle<T> {
	public RectTransform Root { get; }

	private LayoutElement _layout;
	public LayoutElement Layout {
		get {
			if (_layout != null) return _layout;
			_layout = Root.gameObject.GetComponent<LayoutElement>()
				?? Root.gameObject.AddComponent<LayoutElement>();
			return _layout;
		}
	}

	internal ElementHandle(RectTransform root) { Root = root; }

	public T Fixed(float px) {
		var le = Layout;
		le.minWidth = le.preferredWidth = px;
		le.minHeight = le.preferredHeight = px;
		le.flexibleWidth = le.flexibleHeight = 0f;
		return (T)this;
	}

	public T Weight(float w) {
		var le = Layout;
		le.minWidth = le.preferredWidth = 0f;
		le.minHeight = le.preferredHeight = 0f;
		le.flexibleWidth = le.flexibleHeight = w;
		return (T)this;
	}

	public T FixedW(float px) {
		var le = Layout;
		le.minWidth = le.preferredWidth = px;
		le.flexibleWidth = 0f;
		return (T)this;
	}

	public T FixedH(float px) {
		var le = Layout;
		le.minHeight = le.preferredHeight = px;
		le.flexibleHeight = 0f;
		return (T)this;
	}

	public T WeightW(float w) {
		var le = Layout;
		le.minWidth = le.preferredWidth = 0f;
		le.flexibleWidth = w;
		return (T)this;
	}

	public T WeightH(float w) {
		var le = Layout;
		le.minHeight = le.preferredHeight = 0f;
		le.flexibleHeight = w;
		return (T)this;
	}
}
