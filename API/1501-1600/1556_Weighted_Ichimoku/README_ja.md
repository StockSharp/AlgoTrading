# 加重Ichimoku戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はIchimokuのシグナルを加重スコアに統合します。
スコアが買いの閾値を超えたときに買い、スコアが売りの閾値を下回ったときにエグジットします。

## 詳細

- **エントリー条件**: スコア >= BuyThreshold
- **ロング/ショート**: ロングのみ
- **エグジット条件**: スコア <= SellThreshold、または閾値無効時はゼロ以下
- **ストップ**: いいえ
- **デフォルト値**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `Offset` = 26
  - `BuyThreshold` = 60
  - `SellThreshold` = -49
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: Ichimoku
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
