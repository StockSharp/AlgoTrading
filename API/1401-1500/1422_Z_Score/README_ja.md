# Z-Score戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin-AshiのEMAのZ-Scoreを算出し、直近レンジから導出された動的な閾値のクロスに基づいて取引する戦略です。

## 詳細

- **エントリー条件**: スコアが直近の安値を上抜ける、またはスコアのEMAがレンジ中央を上抜ける
- **ロング/ショート**: 両方
- **エグジット条件**: スコアのEMAが直近の高値または安値を下抜ける
- **ストップ**: いいえ
- **デフォルト値**:
  - `HaEmaLength` = 10
  - `ScoreLength` = 25
  - `ScoreEmaLength` = 20
  - `RangeWindow` = 100
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: EMA, SMA, StdDev, Highest, Lowest
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
