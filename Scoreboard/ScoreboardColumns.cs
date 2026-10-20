using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameRow = DiceKingdoms.Interface.ScoreboardRow;
using GameStat = DiceKingdoms.Interface.ScoreboardStat;
using GameButton = DiceKingdoms.Interface.BaseButton;
using GameToolTip = DiceKingdoms.Interface.HasToolTip;

namespace DiceModders.UiKit;

/// <summary>
/// Extra scoreboard cells. Every row of the scoreboard (<c>ScoreboardRow</c>) builds its own stat cells in <c>Start</c>; after that, one more cell per registered
/// column is cloned from the row's last stat cell (the game's own look), stripped of the game's stat logic and fed by the column's text function.
/// </summary>
internal static class ScoreboardColumns {
	private sealed class Cell {
		public ScoreboardColumn Column;
		public GameRow Row;
		public GameObject Go;
		public TMP_Text Text;
		public string Shown;
		public GameObject Trail;   // the row's empty last cell (send-button slot)
		public LayoutElement TrailLayout;
		public Transform SendButton;
		public float TrailWidth = -1f;
	}

	private static readonly List<ScoreboardColumn> Columns = new();
	private static readonly List<GameRow> Rows = new();
	private static readonly List<Cell> Cells = new();
	private static bool _installed;

	public static ScoreboardColumn Add(string id, Func<DiceKingdoms.Game.Island, string> text, string tooltip, Sprite icon) {
		Install();
		Columns.RemoveAll(c => c.Id == id);
		var column = new ScoreboardColumn(id, text, tooltip, icon);
		Columns.Add(column);
		foreach (var row in Rows.ToArray()) if (row != null) EnsureCell(row, column);   // rows that already exist
		return column;
	}

	public static void Remove(ScoreboardColumn column) {
		Columns.Remove(column);
		for (int i = Cells.Count - 1; i >= 0; i--) {
			if (Cells[i].Column != column) continue;
			if (Cells[i].Go != null) UnityEngine.Object.Destroy(Cells[i].Go);
			Cells.RemoveAt(i);
		}
	}

	private static void Install() {
		if (_installed) return;
		_installed = true;
		try { new Harmony("DiceModders.UiKit.scoreboard").PatchAll(typeof(ScoreboardRowStartHook)); }
		catch (Exception e) { UiLog.Error("scoreboard hook failed: " + e); }
	}

	/// <summary>Called by the hook after a row has built its own cells.</summary>
	internal static void RowReady(GameRow row) {
		if (row == null) return;
		Rows.Add(row);
		foreach (var column in Columns.ToArray()) EnsureCell(row, column);
	}

	private static void EnsureCell(GameRow row, ScoreboardColumn column) {
		foreach (var c in Cells) if (c.Row == row && c.Column == column && c.Go != null) return;
		try {
			var stats = row.stats;
			GameStat donor = stats != null && stats.Length > 0 ? stats[stats.Length - 1] : row.firstStat;
			if (donor == null) { UiLog.Once("scoreboard:nodonor", "scoreboard: row has no stat cell to copy"); return; }
			DescribeOnce(donor);

			var go = UnityEngine.Object.Instantiate(donor.gameObject, Staging.Root);   // parked: no game script of the copy runs yet
			go.name = "DM Stat " + column.Id;
			NativeButton.StripForeign(go);
			var logic = go.GetComponent<GameStat>();
			if (logic != null) UnityEngine.Object.DestroyImmediate(logic);              // no game behaviour: the column's function feeds the text
			var gameButton = go.GetComponent<GameButton>();
			if (gameButton != null) UnityEngine.Object.DestroyImmediate(gameButton);
			var unityButton = go.GetComponent<Button>();
			if (unityButton != null) UnityEngine.Object.DestroyImmediate(unityButton);

			var tip = go.GetComponent<GameToolTip>();
			if (string.IsNullOrWhiteSpace(column.Tooltip)) { if (tip != null) UnityEngine.Object.DestroyImmediate(tip); tip = null; }
			else {
				if (tip == null) tip = go.AddComponent<GameToolTip>();
				tip.localizedInitialText = new UnityEngine.Localization.LocalizedString();
				tip.initialText = column.Tooltip;
			}

			TMP_Text text = null;
			foreach (var t in go.GetComponentsInChildren<TMP_Text>(true)) if (t != null) { text = t; break; }
			foreach (var image in go.GetComponentsInChildren<Image>(true)) {
				if (image == null || image.gameObject == go) continue;
				if (!image.name.Contains("Icon")) continue;
				if (column.Icon != null) image.sprite = column.Icon;
				else image.enabled = false;
			}

			// Directly after the row's own stat cells (and after our earlier columns), not at the very end: the row may end with spacers.
			int ours = 0;
			foreach (var c in Cells) if (c.Row == row && c.Go != null) ours++;
			int donorIndex = donor.transform.GetSiblingIndex();
			go.transform.SetParent(donor.transform.parent, false);
			go.transform.SetSiblingIndex(donorIndex + ours);   // before the donor: its own slot (send button) is empty and would leave a gap
			go.SetActive(true);
			if (text != null) NativeText.FitInRect(text, text.fontSize, 8f);   // long text shrinks instead of wrapping into a second line
			if (tip != null) tip.UpdateInitialText(column.Tooltip);

			var trail = donor.gameObject;
			var cell = new Cell { Column = column, Row = row, Go = go, Text = text, Trail = trail };
			var trailLayout = trail.GetComponent<LayoutElement>(); if (trailLayout == null) trailLayout = trail.AddComponent<LayoutElement>();
			cell.TrailLayout = trailLayout; cell.SendButton = trail.transform.Find("Button Send");
			Cells.Add(cell);
			DescribeRowOnce(donor.transform.parent, go.transform);
		}
		catch (Exception e) { UiLog.Error($"scoreboard column '{column.Id}': {e}"); }
	}

	/// <summary>Four times a second (from the runner): refreshes the cell texts and forgets rows that were destroyed.</summary>
	public static void Tick() {
		if (Cells.Count == 0 && Rows.Count == 0) return;
		FitBoard();
		Rows.RemoveAll(r => r == null);
		for (int i = Cells.Count - 1; i >= 0; i--) {
			var c = Cells[i];
			if (c.Go == null || c.Row == null) { Cells.RemoveAt(i); continue; }
			ManageTrail(c);
			if (c.Text == null || !c.Go.activeInHierarchy) continue;
			string value;
			try { value = c.Column.Text(c.Row.Island) ?? ""; }
			catch (Exception e) { value = "?"; UiLog.Once("scoreboard:text:" + c.Column.Id, $"scoreboard column '{c.Column.Id}': {e.Message}"); }
			if (value == c.Shown) continue;
			c.Shown = value;
			c.Text.text = value;
		}
	}

	/// <summary>
	/// The row ends with an empty cell (room for the game's send-resources button). While that button is not shown the cell takes no width, so the extra
	/// column uses its place and the board keeps its original width; if the game shows the button, the cell gets its width back.
	/// </summary>
	private static void ManageTrail(Cell c) {
		try {
			if (c.Trail == null) return;
			var rt = c.Trail.GetComponent<RectTransform>();
			if (rt == null) rt = c.Trail.transform.TryCast<RectTransform>();
			if (rt == null) return;
			if (c.TrailWidth < 0f) c.TrailWidth = rt.sizeDelta.x > 1f ? rt.sizeDelta.x : rt.rect.width;
			if (Mathf.Abs(rt.sizeDelta.x) < 0.01f && c.TrailLayout != null && Mathf.Approximately(c.TrailLayout.preferredWidth, 0f)) return;   // already collapsed
			if (c.TrailLayout != null) { c.TrailLayout.preferredWidth = 0f; c.TrailLayout.minWidth = 0f; c.TrailLayout.flexibleWidth = 0f; }
			rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);   // row layouts that do not control child widths read the size itself
			LayoutRebuilder.MarkLayoutForRebuild(c.Trail.transform.parent.TryCast<RectTransform>());
			UiLog.Once("scoreboard:trail-collapsed", $"scoreboard: the row's empty last cell was collapsed (was {c.TrailWidth:0} wide; send button active={(c.SendButton != null && c.SendButton.gameObject.activeInHierarchy)})");
		}
		catch (Exception e) { UiLog.Once("scoreboard:trail", "scoreboard trail: " + e.Message); }
	}

	private static RectTransform _board;
	private const float SideMargin = 260f;   // the left victory panels end at ~240 canvas units

	/// <summary>The board is centered and grows with every extra column; shrink it just enough to stay clear of the side panels.</summary>
	private static void FitBoard() {
		try {
			if (_board == null) {
				foreach (var c in Cells) {
					if (c.Go == null) continue;
					for (var t = c.Go.transform.parent; t != null; t = t.parent)
						if (t.name == "Scores") { _board = t.TryCast<RectTransform>(); break; }
					if (_board != null) break;
				}
			}
			if (_board == null) return;
			var root = _board.root.TryCast<RectTransform>();
			if (root == null) return;
			float current = Mathf.Max(0.01f, _board.localScale.x);
			float width = RectTransformUtility.CalculateRelativeRectTransformBounds(root, _board).size.x / current;   // the frame is a child of Scores
			if (width < 1f) return;
			float scale = Cells.Count == 0 ? 1f : Mathf.Clamp((root.rect.width - 2f * SideMargin) / width, 0.5f, 1f);
			if (Mathf.Abs(_board.localScale.x - scale) > 0.005f) _board.localScale = new Vector3(scale, scale, 1f);
		}
		catch (Exception e) { UiLog.Once("scoreboard:fit", "scoreboard fit: " + e.Message); }
	}

	/// <summary>One-time dump of the row and its ancestors (layout components, children with sizes) - to see how the board is laid out.</summary>
	private static void DescribeRowOnce(Transform rowParent, Transform ours) {
		var sb = new System.Text.StringBuilder("scoreboard row layout (new cell '" + ours.name + "' at index " + ours.GetSiblingIndex() + "):");
		int depth = 0;
		for (var t = rowParent; t != null && depth < 4; t = t.parent, depth++) {
			var rt = t.TryCast<RectTransform>();
			sb.Append($"\n  [{depth}] {t.name}: rect={(rt != null ? rt.rect.ToString() : "-")} anchors={(rt != null ? rt.anchorMin + "-" + rt.anchorMax : "-")} scale={t.localScale.x:0.##} comps=");
			foreach (var c in t.GetComponents<Component>()) if (c != null) sb.Append(c.GetType().Name).Append(' ');
			if (depth == 0) {
				for (int i = 0; i < t.childCount; i++) {
					var ch = t.GetChild(i).TryCast<RectTransform>();
					if (ch == null) continue;
					var le = ch.GetComponent<LayoutElement>();
					sb.Append($"\n      #{i} {ch.name} active={ch.gameObject.activeSelf} size={ch.rect.width:0}x{ch.rect.height:0} pos={ch.anchoredPosition}" +
						(le != null ? $" layout(min={le.minWidth:0},pref={le.preferredWidth:0},flex={le.flexibleWidth:0})" : ""));
				}
			}
		}
		UiLog.Once("scoreboard-row-layout", sb.ToString());
	}

	private static void DescribeOnce(GameStat donor) {
		var sb = new System.Text.StringBuilder("scoreboard stat cell structure ('" + donor.name + "' in '" + (donor.transform.parent != null ? donor.transform.parent.name : "-") + "'):");
		foreach (var r in donor.GetComponentsInChildren<RectTransform>(true)) {
			if (r == null) continue;
			sb.Append($"\n   {r.name}: rect={r.rect} anchors={r.anchorMin}-{r.anchorMax} image={(r.GetComponent<Image>() != null ? "yes" : "no")} text={(r.GetComponent<TMP_Text>() != null ? "yes" : "no")}");
		}
		UiLog.Once("scoreboard-structure", sb.ToString());
	}
}

[HarmonyPatch(typeof(GameRow), "Start")]
internal static class ScoreboardRowStartHook {
	[HarmonyPostfix]
	public static void Postfix(GameRow __instance) {
		try { ScoreboardColumns.RowReady(__instance); }
		catch (Exception e) { UiLog.Once("scoreboard:row", "scoreboard row hook: " + e); }
	}
}
