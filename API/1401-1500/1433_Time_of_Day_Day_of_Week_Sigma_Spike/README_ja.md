# 時間帯・曜日別シグマスパイク戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

リターンのzスコアを使用して、時間別の大きな価格変動をオプションの曜日フィルターで強調表示します。
スパイク時にエントリーし、ボラティリティが正常化したときに決済します。

## 詳細

- **エントリー条件**: 絶対zスコア >= `Threshold`
- **ロング/ショート**: ロングのみ
- **エグジット条件**: zスコアが`Threshold`を下回る
- **ストップ**: いいえ
- **デフォルト値**:
  - `Threshold` = 2.5
  - `AllDays` = false
  - `DayOfWeekFilter` = Monday
  - `StdevLength` = 20
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: ロング
  - インジケーター: StandardDeviation
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: はい
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
