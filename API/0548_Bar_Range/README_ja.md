# バーレンジ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bar Range戦略は、現在のバーのレンジが直近のバーの中で最も高い水準にあり、かつローソク足が始値を下回って終値をつけたときにロングエントリーします。ポジションは固定本数のバー後にクローズされます。

## 詳細

- **エントリー条件**:
  - レンジ = High − Low
  - `LookbackPeriod` におけるレンジのパーセントランク ≥ `PercentRankThreshold`
  - Close < Open
- **エグジット条件**: `ExitBars` 本後にポジションをクローズ。
- **デフォルト値**:
  - `LookbackPeriod` = 50
  - `PercentRankThreshold` = 95
  - `ExitBars` = 1
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: ロングのみ
  - インジケーター: Percent Rank
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
