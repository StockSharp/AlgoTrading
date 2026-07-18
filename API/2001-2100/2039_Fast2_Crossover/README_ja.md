# Fast2クロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Fast2ヒストグラムに基づく戦略です。ヒストグラムは直近3本のローソク足の実体を平方根のウェイトで組み合わせ、2本の加重移動平均を適用します。速い平均が遅い平均を下抜けするとロングポジションが開かれ、上抜けするとショートポジションが開かれます。

## 詳細

- **エントリー条件**:
  - ロング: 速いWMAが遅いWMAを下抜けする
  - ショート: 速いWMAが遅いWMAを上抜けする
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 反対のクロスオーバー
- **ストップ**: なし
- **デフォルト値**:
  - `FastLength` = 3
  - `SlowLength` = 9
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **フィルター**:
  - カテゴリ: クロスオーバー
  - 方向: 両方
  - インジケーター: WeightedMovingAverage
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
