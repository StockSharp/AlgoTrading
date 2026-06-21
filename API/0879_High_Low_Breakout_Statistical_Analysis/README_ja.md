# 高値安値ブレイクアウト統計分析戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

選択した時間軸の高値または安値レベルのブレイクアウトを取引します。設定オプションに基づいてロングまたはショートでエントリーし、固定バー数後にポジションを決済します。

## 詳細

- **エントリー条件**: 終値が選択した高値または安値レベルを越える。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはHoldingPeriodバー後。
- **ストップ**: なし。
- **デフォルト値**:
  - `EntryOption` = LongAtHigh
  - `TimeframeOption` = Daily
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: High, Low
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
