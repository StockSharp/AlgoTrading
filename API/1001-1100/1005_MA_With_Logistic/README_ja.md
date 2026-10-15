# ロジスティック関数付きMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

歴史的な名称とは異なり、この実装は 2 本の EMA のクロス戦略であり、ロジスティックモデルは計算しません。確定足でのクロスがポジションを新規または反転し、約定価格からの割合で利確と損切りを管理します。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: 速い EMA が遅い EMA を上抜ける。
  - **ショート**: 速い EMA が遅い EMA を下抜ける。
- **エグジット条件**: `TakeProfitPercent` または `StopLossPercent` に到達する。逆方向のクロスではポジションを反転する。
- **クールダウン**: 各クロス注文の後、確定足 5 本。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 25
  - `TakeProfitPercent` = 8
  - `StopLossPercent` = 5
  - `CandleType` = 20 分
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングとショート
  - インジケーター: MA
  - 複雑さ: 低
  - リスクレベル: 中
