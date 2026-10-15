# US30 Stealth戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

移動平均の傾き、エンガルフィングパターン、出来高、セッションフィルターを使用したUS30のプライスアクション戦略。
ポジションサイズは1トレードのリスクから算出し、ストップロスとテイクプロフィットはローソク足のレンジに基づく。

## 詳細

- **エントリー条件**: トレンド方向、3本の連続する低い高値または高い安値、エンガルフィングパターン、出来高と時間フィルター。
- **ロング/ショート**: 両方
- **エグジット条件**: テイクプロフィットまたはストップロス
- **ストップ**: 固定
- **デフォルト値**:
  - `MaLen` = 50
  - `VolMaLen` = 20
  - `HlLookback` = 5
  - `RrRatio` = 2.2
  - `MaxCandleSize` = 30
  - `PipValue` = 1
  - `RiskAmount` = 50
  - `LargeCandleThreshold` = 25
  - `MaSlopeLen` = 3
  - `MinSlope` = 0.1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: Price action
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
