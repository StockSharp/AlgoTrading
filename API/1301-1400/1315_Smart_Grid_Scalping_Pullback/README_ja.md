# スマートグリッドスキャルピングプルバック戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

20バー前のベース価格からATRベースの価格レベルを展開するグリッドベースのスキャルピング戦略です。エントリー前にプルバックをRSIでフィルタリングします。ポジションは利益目標とATRトレーリングストップを使用します。

## 詳細

- **エントリー条件**:
  - ロング: close < basePrice - (LongLevel + 1) * ATR * GridFactor && range/low > NoTradeZone && RSI < MaxRsiLong && close > open
  - ショート: close > basePrice + (ShortLevel + 1) * ATR * GridFactor && range/high > NoTradeZone && RSI > MinRsiShort && close < open
- **ロング/ショート**: 両方
- **エグジット条件**: 利益目標またはATRトレーリングストップ
- **ストップ**: ATRトレーリングストップ
- **デフォルト値**:
  - `AtrLength` = 10
  - `GridFactor` = 0.35m
  - `ProfitTarget` = 0.004m
  - `NoTradeZone` = 0.003m
  - `ShortLevel` = 5
  - `LongLevel` = 5
  - `MinRsiShort` = 70
  - `MaxRsiLong` = 30
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: スキャルピング
  - 方向: 両方
  - インジケーター: ATR, RSI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
