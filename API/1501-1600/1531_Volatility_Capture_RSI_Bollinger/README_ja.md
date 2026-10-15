# ボラティリティ・キャプチャー RSI-Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は動的なボリンジャーバンドとオプションのRSIフィルターを組み合わせ、ボラティリティの変動を捉えます。

## 詳細
- **エントリー条件**: オプションのRSI確認を伴うアダプティブ・ボリンジャーバンドをPrice が突破すること。
- **ロング/ショート**: `Direction` で設定可能。
- **エグジット条件**: Priceがトレーリングバンドの反対側を突破すること。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `BollingerLength` = 50
  - `Multiplier` = 2.7183m
  - `UseRsi` = true
  - `RsiPeriod` = 10
  - `RsiSmaPeriod` = 5
  - `BoughtRangeLevel` = 55m
  - `SoldRangeLevel` = 50m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: 設定可能
  - インジケーター: Bollinger, RSI
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
