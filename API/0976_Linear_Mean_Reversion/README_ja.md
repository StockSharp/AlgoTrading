# 線形平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

線形平均回帰戦略は、移動平均に対する価格の z スコアを使用して、固定のポイント数でストップロスを設定した平均回帰取引を行います。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: z-score < -EntryThreshold。
  - **ショート**: z-score > EntryThreshold。
- **エグジット条件**: z スコアがゼロ方向に戻る（ロングは z-score > -ExitThreshold、ショートは z-score < ExitThreshold）。
- **ストップ**: 固定ポイントのストップロス。
- **デフォルト値**:
  - `HalfLife` = 14
  - `Scale` = 1
  - `EntryThreshold` = 2
  - `ExitThreshold` = 0.2
  - `StopLossPoints` = 50
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロング & ショート
  - インジケーター: SMA, StandardDeviation
  - ストップ: はい
  - 複雑さ: 低
  - リスクレベル: 中
