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
- `Technologies.TechTree(Civilization)` is `First(t => t.civilization == civ)`; a prefix returns the registered provider's tree for registered ids.
- A modded civilization also needs whatever else the game looks up per civilization (e.g. `PlayerSkins`/`SkinRegistry` entries); that data belongs to the mod.
- `ScoreboardRow.Start` creates the row's 10 stat cells (ping, 6 resources, population, technologies) by cloning `stats[0]`. A postfix clones the last cell once per
  registered column, destroys the clone's `ScoreboardStat` (so the game does not drive it) and writes the text from the column's function four times a second.
  The first cell built logs its structure once (`scoreboard stat cell structure ...`).
