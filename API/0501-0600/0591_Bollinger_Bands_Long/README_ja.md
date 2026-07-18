# Bollinger Bands ロング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格がBollinger Bands下限を下回って終値が付き、RSIが売られすぎの状態にある時に買いを入れます。価格が中央バンドに戻ったらロングポジションを決済します。

## 詳細

- **エントリー条件**:
  - 価格がBollinger Bands下限を下回って終値が付く。
  - RSIが売られすぎのレベルを下回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - 価格がBollinger Bands中央バンドで、またはそれを上回って終値が付く。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `BbLength` = 10
  - `BbDeviation` = 2
  - `RsiLength` = 14
  - `RsiOversold` = 30
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロング
  - インジケーター: Bollinger Bands, RSI
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
