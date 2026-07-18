# SuperTrend 強化ピボット リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SuperTrendの方向とピボット高値/安値のブレイクアウトを組み合わせます。SuperTrendが弱気のとき、直近のピボット高値の上にロングストップを置きます。SuperTrendが強気のとき、ピボット安値の下にショートストップを置きます。ポジションはピボットからのパーセンテージストップロスで保護されます。

## 詳細

- **エントリー条件**:
  - ロング: ピボット高値が形成、SuperTrendが下向き → ピボット上にバイストップ。
  - ショート: ピボット安値が形成、SuperTrendが上向き → ピボット下にセルストップ。
- **方向**: 設定可能。
- **エグジット条件**: パーセンテージストップロスまたは片側モードの反対方向。
- **インジケーター**: SuperTrend、ピボット高値/安値。
- **デフォルト値**:
  - `LeftBars` = 6
  - `RightBars` = 3
  - `AtrLength` = 5
  - `Factor` = 2.618
  - `StopLossPercent` = 20
  - `TradeDirection` = Both
  - `CandleType` = 5 minute
