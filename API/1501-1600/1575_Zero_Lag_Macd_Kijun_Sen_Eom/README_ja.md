# Zero Lag MACD + Kijun-sen + EOM戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Zero Lag MACDとKijun-senベースライン、Ease of Movementフィルターを組み合わせた戦略。ATRベースのストップとテイクプロフィットを使用する。

## 詳細

- **エントリー条件**: Kijun-senとEOMフィルターを伴うMACDクロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATRベースのストップまたはテイクプロフィット。
- **ストップ**: あり。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdEmaLength` = 9
  - `KijunPeriod` = 26
  - `EomLength` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.5m
  - `RiskReward` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD, Donchian, EOM, ATR
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
