# ExpXmaRangeBands 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、StockSharpの高レベルAPIを使用してMetaTraderサンプル「Exp_XMA_Range_Bands」のロジックを再現したものです。移動平均と平均真のレンジに基づいた動的なサポートとレジスタンスを定義するためにKeltnerチャネルを使用します。価格がチャネル外に移動した後に再びチャネルに戻ったときにトレードが発動されます。

## 仕組み

1. 以下を使用してKeltnerチャネルを構築する：
   - EMA期間 `MaLength`
   - ATR期間 `RangeLength`
   - ATR乗数 `Deviation`
2. ローソク足が前回の上位バンドを超えて終値をつけた場合、既存のショートポジションをクローズします。次のローソク足がチャネル内に戻って終値をつけた場合（終値 ≤ 現在の上位バンド）、ロングポジションを開きます。
3. ローソク足が前回の下位バンドを下回って終値をつけた場合、既存のロングポジションをクローズします。次のローソク足がチャネル内に戻って終値をつけた場合（終値 ≥ 現在の下位バンド）、ショートポジションを開きます。
4. ストップロスとテイクプロフィットのレベルはポイントで表現され、ポジション開設後に適用されます。

## パラメーター

- `MaLength` – チャネル中心のEMA期間。
- `RangeLength` – チャネル幅に使用するATR期間。
- `Deviation` – バンド計算のためにATRに適用する乗数。
- `StopLoss` – ポイントでのストップロス（`Security.PriceStep`で価格に変換）。
- `TakeProfit` – ポイントでのテイクプロフィット（`Security.PriceStep`で価格に変換）。
- `CandleType` – 計算に使用するローソク足シリーズ。

## インジケーター

- KeltnerChannels (EMA + ATR)
