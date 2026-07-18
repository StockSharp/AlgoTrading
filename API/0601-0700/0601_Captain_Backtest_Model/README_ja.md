# Captain バックテストモデル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

序盤のセッション価格レンジを追跡して日次バイアスを確立します。戻り後の取引ウィンドウ中にブレイクアウトを取引します。

## 詳細

- **バイアス**: 午前レンジの高値または安値がロングまたはショートのバイアスを決定します。
- **エントリー**: 戻り条件が満たされた後、前のローソク足を上抜け/下抜けでブレイクアウト。
- **ロング/ショート**: 両方。
- **エグジット**: 固定リスク/リワードまたは取引ウィンドウの終了。
- **ストップ**: 固定ポイント距離。
- **デフォルト値**:
  - PrevRangeStart = 06:00
  - PrevRangeEnd = 10:00
  - TakeStart = 10:00
  - TakeEnd = 11:15
  - TradeStart = 10:00
  - TradeEnd = 16:00
  - Risk = 25
  - Reward = 75
