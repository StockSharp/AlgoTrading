# Z-Score Normalized VIX戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

複数のVIX指数のZ-Scoreを平均化し、組み合わせた値が負の閾値を下回るとロングエントリーする戦略。

アルゴリズムはVIX、VIX3M、VIX9D、VVIXのZ-Scoreを計算する。選択されたZ-Scoreを平均化して、全体的なボラティリティセンチメントを表す単一の指標を形成する。

## 詳細

- **エントリー条件**: 組み合わせZ-Scoreが`-Threshold`を下回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 組み合わせZ-Scoreが`-Threshold`を上回る。
- **ストップ**: なし。
- **デフォルト値**:
  - `ZScoreLength` = 6
  - `Threshold` = 1
  - `UseVix` = true
  - `UseVix3m` = true
  - `UseVix9d` = true
  - `UseVvix` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: ロング
  - インジケーター: Z-Score
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
