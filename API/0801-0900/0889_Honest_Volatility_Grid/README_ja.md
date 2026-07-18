# Honest Volatility Grid 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

複数のKeltner Channelレベルを使ってボラティリティグリッドを構築する戦略です。事前に定義されたバンドでロングおよびショートポジションをスケールインし、反対側のレベルまたは生のストップでエグジットします。

## 詳細

- **エントリー条件**: 価格が設定されたKeltnerチャネルのレベルに達する。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対側のチャネルまたは生のストップ。
- **ストップ**: オプションの生のストップ。
- **デフォルト値**:
  - `EmaPeriod` = 200
  - `Multiplier` = 1.0
  - `LEntry1Level` = -2
  - `SEntry1Level` = 2
  - `RawStopLevel` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: グリッド
  - 方向: 両方
  - インジケーター: EMA, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
