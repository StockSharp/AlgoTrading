# Khaled TamimのAvellaneda-Stoikov戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Avellaneda-Stoikovのマーケットメイキングモデルを実装します。直近2本の終値から買い値と売り値を計算し、価格が設定可能なマージンを超えて乖離したときに成行注文を出します。

## 詳細

- **エントリー条件**:
  - **ロング**: `close < bidQuote - M`
  - **ショート**: `close > askQuote + M`
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `Gamma` = 2
  - `Sigma` = 8
  - `T` = 0.0833
  - `K` = 5
  - `M` = 0.5
  - `Fee` = 0
- **フィルター**:
  - カテゴリ: マーケットメイキング
  - 方向: 両方
  - インジケーター: なし
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
