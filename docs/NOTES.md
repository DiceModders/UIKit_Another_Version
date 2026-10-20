# Implementation notes
- Game types (namespaces `DiceKingdoms.Interface` / `DiceKingdoms.Meta`): BaseButton, ButtonIcon, ButtonLabel, Label, HasToolTip, Toggle, SettingSetInterface, SettingEntryInterface, SettingSetsInterface, SettingInterfaceBool.
- Top-right bars: `Meta/Buttons` (match), `Top Right/Top Right Buttons` (menu).
- Selected = normal art; unselected = disabled art overlay hidden on hover/press.
- Delegates passed to the game are kept alive with the Unity object they belong to (`Il2CppDelegates.Hold`).
- Not yet verified in game: exact geometry of `SettingEntryInterface.contentHolder` and the toggle prefab; check the log first if rows look off.
