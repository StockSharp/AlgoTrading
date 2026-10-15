# Megabar ブレイクアウト戦略（Range・出来高・RSI）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Megabar Breakoutは、高出来高とRSI確認に裏付けられた大きなローソク足を検出します。強気のMegabarでロング、弱気のMegabarでショートにエントリーします。

平均レンジと出来高を乗算してMegabarを特定します。RSIの移動平均がトレードをフィルタリングします。

## 詳細

- **エントリー条件**: ローソク足の実体と出来高が指定した乗数分だけそれぞれの移動平均を超える。RSI MAが買いのロング閾値を上回り、売りのショート閾値を下回る。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロスまたはテイクプロフィット。
- **ストップ**: はい。
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VolumeAveragePeriod` = 20
  - `VolumeMultiplier` = 3
  - `RangeAveragePeriod` = 20
  - `RangeMultiplier` = 4
  - `RsiPeriod` = 14
  - `RsiMaPeriod` = 14
  - `LongRsiThreshold` = 50
  - `ShortRsiThreshold` = 70
  - `TakeProfit` = 400
  - `StopLoss` = 300
  - `FilterTradeHours` = false
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: 出来高、Range、RSI
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
