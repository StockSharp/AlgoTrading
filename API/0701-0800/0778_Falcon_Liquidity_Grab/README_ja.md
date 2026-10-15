# Falcon Liquidity Grab戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、シンプル移動平均でトレンドを定義しながら、主要な市場セッション中の流動性グラブをトレードします。価格が直近のスイングレベルを越えてトレンドとともに反転するときにエントリーします。各トレードはティック単位の固定ストップロスとテイクプロフィットを使用します。

## 詳細

- **エントリー条件**:
  - **ロング**: `Low < lowest(swing period)` && `Close > SMA` && `session filter`
  - **ショート**: `High > highest(swing period)` && `Close < SMA` && `session filter`
- **エグジット条件**: 固定ストップロスとテイクプロフィット。
- **タイプ**: リバーサル
- **インジケーター**: SMA、Highest、Lowest
- **時間軸**: 15分（デフォルト）
- **ストップ**: `StopLossPoints` ティック、`TakeProfitMultiplier`× ストップ距離
