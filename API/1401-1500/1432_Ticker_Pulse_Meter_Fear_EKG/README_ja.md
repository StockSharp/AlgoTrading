# Ticker Pulse Meter + Fear EKG戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

短期と長期のルックバックを組み合わせて売られすぎの状態と回復を検出します。
組み合わせたパーセンタイルが上限トリガーを上回るとエントリーし、利確クロスで決済します。

## 詳細

- **エントリー条件**: パーセンタイルが`EntryThresholdHigh`を上回るか`OrangeEntryThreshold`を下回る
- **ロング/ショート**: ロングのみ
- **エグジット条件**: `ProfitTake`を下回るクロス
- **ストップ**: いいえ
- **デフォルト値**:
  - `LookbackShort` = 50
  - `LookbackLong` = 200
  - `ProfitTake` = 95
  - `EntryThresholdHigh` = 20
  - `EntryThresholdLow` = 40
  - `OrangeEntryThreshold` = 95
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: ロング
  - インジケーター: Highest, Lowest
  - ストップ: いいえ
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
