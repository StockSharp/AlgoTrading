# スキャルピング EMA RSI MACD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

30分足スキャルピング戦略で、高速/低速EMAクロスオーバー、トレンドEMA、RSIとMACDフィルター、出来高条件を組み合わせます。ストップロスはATRベースで、テイクプロフィットは固定リスク・リワード比を使用します。

## 詳細

- **エントリー条件**: トレンド方向への高速EMAが低速EMAをクロス、RSIが範囲内、MACD確認および高出来高。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対側のストップまたはターゲット達成。
- **ストップ**: ATRベースのストップロスとリスク・リワードによるテイクプロフィット。
- **デフォルト値**:
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
  - `TrendEmaLength` = 55
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `VolumeMaLength` = 20
  - `VolumeThreshold` = 1.3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **フィルター**:
  - カテゴリ: Scalping
  - 方向: 両方
  - インジケーター: EMA, RSI, MACD, ATR, Volume
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (30m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
