# Implementation notes
- Game types (namespaces `DiceKingdoms.Interface` / `DiceKingdoms.Meta`): BaseButton, ButtonIcon, ButtonLabel, Label, HasToolTip, Toggle, SettingSetInterface, SettingEntryInterface, SettingSetsInterface, SettingInterfaceBool.
- Top-right bars: `Meta/Buttons` (match), `Top Right/Top Right Buttons` (menu).
- Selected = normal art; unselected = disabled art overlay hidden on hover/press.
- Delegates passed to the game are kept alive with the Unity object they belong to (`Il2CppDelegates.Hold`).
- Not yet verified in game: exact geometry of `SettingEntryInterface.contentHolder` and the toggle prefab; check the log first if rows look off.


## Rules screen and scoreboard (how it works)

- `Rules.Awake` fills `civilizationDropdown` (a `DiceKingdoms.Interface.Dropdown`) with `(StructuredText, boxed Civilization)` tuples from `Enum.GetValues(typeof(Civilization))`.
  The wrapper keeps the boxed values in `values` and its TMP_Dropdown calls `onSelect(values[index])`; `Rules.OnSelectCivilization(object)` unboxes a `Civilization`
  and sets `PlayerSkins.Civilization`, then the tech tree tab is rebuilt with `Technologies.TechTree(civilization)`.
  UiKit therefore appends an option to the TMP_Dropdown and a boxed enum value (`Enum.ToObject`) to `values` - a postfix on `Rules.Awake`/`OnEnable`, nothing is polled.
- Only the drop-down entry is UiKit's. What the game needs to accept a civilization beyond 0..4 (tech tree lookup, skins) is game logic and lives in ModCore.
- `ScoreboardRow.Start` creates the row's 10 stat cells (ping, 6 resources, population, technologies) by cloning `stats[0]`. A postfix clones the last cell once per
  registered column, destroys the clone's `ScoreboardStat` (so the game does not drive it) and writes the text from the column's function four times a second.
  The first cell built logs its structure once (`scoreboard stat cell structure ...`).


## Wrapping text and the title bar

- Auto-height labels (`Paragraph`, `Wrap()`, `AutoHeight()`) measure themselves: `LabelHandle.Measure` asks TMP for the preferred height at the width the label really has
  (`GetPreferredValues(text, width, 0)`) and writes it into the label's `LayoutElement`, then marks the parent for a layout rebuild. `TextWrap.Frame()` (from the runner, every frame)
  repeats this when the width, text or font size changed, so a window resize, a scrollbar appearing or `SetText` all converge in a frame or two. Such a label has no pixel width of its own
  (min/preferred 0, flexible 1) so it never pushes a row wider.
- `Title()` / `TitleBar` create `DM Title Bar` (top 10, height 32) with a `HorizontalLayoutGroup`: the title text (flexible width, ellipsis) and `DM Title Buttons`. The body's top offset moves
  below the strip. The strip's right inset is 16, or 50 with `Closable()` (the X is 31.75 wide, 10 from the edge); `Closable()` also creates the strip, so the X always has a row; both calls update it, so the order does not matter.
- Buttons made on `TitleBar` are as wide as their text (`TextSizedButtons`); everywhere else `Button` still shares the row (`WeightW(1)`).
- Il2Cpp note: `Transform.parent` returns a `Transform` wrapper, so use `TryCast<RectTransform>()` (a plain `as RectTransform` gives null).

## Buttons, icons and templates

- Icon buttons in windows are a clone of the text button (Settings "Close") with an image in place of the text: the top-bar button art is designed for 48 units and
  its 9-slice borders overlap (hairlines) when squeezed to 32. Only the top bar (`NativeButton.CreateBarIcon`) uses that donor, at its native size.
- Button height is `NativeButton.SlicedFit` (31.75): at exactly 32 the centre slice of the Button-1 art is empty and rounding leaves hairlines.
- There are no hand-built fallbacks any more: when a template of the game is missing, creating the widget throws a clear error (create windows after the game UI is up).
- A text cut with "..." (TMP `Ellipsis`) shows nothing if its box is shorter than one line, so `Label` sizes itself from the font's line height (`LabelHandle.LineHeight`).
