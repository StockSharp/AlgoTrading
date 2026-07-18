# Hurst Future Lines of Demarcation戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、平滑化されたFLD（Future Line of Demarcation）と3つのサイクル長（シグナル、トレード、トレンド）を使用します。特定のトレンド状態でシグナルFLDを価格がクロスした際にエントリーし、選択された値のクロスでエグジットします。

## 詳細

- **エントリー条件**:
  - トレンド状態が1の間に価格がシグナルFLDを上抜けたときに買い。
  - トレンド状態が6の間に価格がシグナルFLDを下抜けたときに売り。
- **ロング/ショート**: 両方。
- **エグジット条件**: トレードの反対方向に`CloseTrigger1`が`CloseTrigger2`をクロスしたときにポジションを閉じる。
- **ストップ**: なし。
- **デフォルト値**:
  - `SmoothFld` = false
  - `FldSmoothing` = 5
  - `SignalCycleLength` = 5
  - `TradeCycleLength` = 20
  - `TrendCycleLength` = 80
  - `CloseTrigger1` = Price
  - `CloseTrigger2` = Trade
