# ICT NY Kill Zone自動売買戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

フェアバリューギャップとオーダーブロックを使用して、ニューヨークのキルゾーン中に取引する戦略です。

## 詳細

- **エントリー条件**: キルゾーン内のフェアバリューギャップとオーダーブロック。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ポジション保護。
- **ストップ**: はい。
- **デフォルト値**:
  - `StopLoss` = 30
  - `TakeProfit` = 60
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Price Action
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

