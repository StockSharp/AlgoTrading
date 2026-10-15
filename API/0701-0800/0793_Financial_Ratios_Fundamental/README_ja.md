# 財務指標ファンダメンタル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は四半期財務指標を分析して企業のファンダメンタルズを評価します。流動比率、インタレストカバレッジ、買掛金回転率、粗利益率を確認し、これらの指標のいずれかが前期に比べて改善したときにロングポジションを取ります。

## 詳細

- **エントリー条件**:
  - **ロング**: `currentRatio > previousCurrent` または `interestCoverage < previousInterest` または `payableTurnover > previousPayable` または `grossMargin > previousGross`。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - **ロング**: `currentRatio < previousCurrent` または `interestCoverage > previousInterest` または `payableTurnover < previousPayable` または `grossMargin < previousGross`。
- **ストップ**: なし。
- **デフォルト値**:
  - `Candle Type` = 日足ローソク足。
- **フィルター**:
  - カテゴリ: ファンダメンタル
  - 方向: ロングのみ
  - インジケーター: なし
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 長期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
