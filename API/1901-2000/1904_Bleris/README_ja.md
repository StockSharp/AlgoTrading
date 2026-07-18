# Bleris戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Bleris戦略は、最近の価格の極値のトレンドを分析し、現在のトレンドの方向にトレードを開始します。
価格系列は`SignalBarSample`の長さの3つのセグメントに分割され、これらのセグメントの最高値と最安値が比較されます。

- **インジケーター**: Highest, Lowest
- **パラメーター**:
  - `SignalBarSample` – セグメントあたりのローソク足の数。
  - `CounterTrend` – 取引方向を反転させる。
  - `Lots` – 注文数量。
  - `CandleType` – ローソク足の時間軸。
  - `AnotherOrderPips` – 同じタイプの別の注文を開く前の最小距離（pips単位）。

## 動作原理
1. HighestとLowestインジケーターが最後の`SignalBarSample`本のローソク足の極値価格を計算します。
2. 高値の低下は下降トレンドを示し、安値の上昇は上昇トレンドを示します。
3. 上昇トレンドでは買い、下降トレンドでは売りです。`CounterTrend`が有効な場合はロジックが反転します。
4. 最後の注文価格が`AnotherOrderPips`以内の場合、同方向の新規注文は無視されます。

このサンプルはStockSharpの高水準APIを使用し、教育目的を意図しています。
