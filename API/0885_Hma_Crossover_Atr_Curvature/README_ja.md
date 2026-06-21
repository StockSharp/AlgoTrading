# HMA クロスオーバー ATR カーブチャー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

HMA Crossover ATR Curvatureは、高速と低速のHull Moving Averageのクロスオーバーにカーブチャーフィルターを組み合わせたトレンドフォロー戦略です。ポジションサイズはATRとリスク割合に基づいており、ATRベースのトレーリングストップで取引を保護します。

## 詳細
- **データ**: 価格ロウソク足。
- **エントリー条件**:
  - **ロング**: 高速HMAが低速HMAを上抜けし、カーブチャーが閾値を上回る。
  - **ショート**: 高速HMAが低速HMAを下抜けし、カーブチャーが負の閾値を下回る。
- **エグジット条件**: ATR トレーリングストップ。
- **ストップ**: ATR トレーリングストップ。
- **デフォルト値**:
  - `FastLength` = 15
  - `SlowLength` = 34
  - `AtrLength` = 14
  - `RiskPercent` = 1
  - `AtrMultiplier` = 1.5
  - `TrailMultiplier` = 1
  - `CurvatureThreshold` = 0
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング & ショート
  - インジケーター: HMA, ATR
  - 複雑さ: 低
  - リスクレベル: 中
