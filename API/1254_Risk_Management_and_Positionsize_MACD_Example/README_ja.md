# リスク管理とポジションサイズ - MACDの例
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**リスク管理とポジションサイズ - MACDの例**戦略は、現在の資産残高に基づく動的なポジションサイズ管理を示す。上位時間軸のMACDクロスオーバーと移動平均トレンドフィルターを組み合わせる。

## 詳細
- **エントリー条件**: トレンド確認を伴い、MACDラインがシグナルラインを上抜け/下抜けする。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆方向のMACDクロスオーバー。
- **ストップ**: なし。
- **デフォルト値**:
  - `InitialBalance = 10000m`
  - `LeverageEquity = true`
  - `MarginFactor = -0.5m`
  - `Quantity = 3.5m`
  - `MacdMaType = MovingAverageTypeEnum.EMA`
  - `FastMaLength = 11`
  - `SlowMaLength = 26`
  - `SignalMaLength = 9`
  - `MacdTimeFrame = TimeSpan.FromMinutes(30)`
  - `TrendMaType = MovingAverageTypeEnum.EMA`
  - `TrendMaLength = 55`
  - `TrendTimeFrame = TimeSpan.FromDays(1)`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MACD, Moving Average
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
