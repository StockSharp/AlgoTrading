# グリッドボット・バックテスト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がグリッドレベルに達したときにロングポジションを積み上げ、価格が次のラインに移動したときに決済するグリッドトレーディングボットを実装します。境界は手動で設定するか、最近のデータから計算できます。

## 詳細

- **エントリー条件**:
  - **ロング**: アクティブな注文がないグリッドラインを価格が下回る
- **ロング/ショート**: ロングのみ
- **エグジット条件**:
  - 価格が次のグリッドラインを上回る
- **ストップ**: なし
- **デフォルト値**:
  - `AutoBounds` = true
  - `BoundSource` = "Hi & Low"
  - `BoundLookback` = 250
  - `BoundDeviation` = 0.10
  - `UpperBound` = 0.285
  - `LowerBound` = 0.225
  - `GridLines` = 30
- **フィルター**:
  - カテゴリ: レンジトレード
  - 方向: ロングのみ
  - インジケーター: Highest, Lowest, SimpleMovingAverage
  - ストップ: いいえ
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
