# RSI Stochastic MA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は単純移動平均（SMA）トレンドフィルターをRSIおよびStochasticオシレーターと組み合わせます。
移動平均が市場のバイアスを定義します。価格がSMAより上にある場合、戦略はロングエントリーを探し、
SMAより下にある場合はショートエントリーを探します。RSIとStochasticのレベルが売られすぎや
買われすぎの状態を識別してエントリーのタイミングを計ります。

オシレーターが極端なゾーンを抜けるとポジションがクローズされます。これにより取引は
支配的なトレンドに沿って維持され、インジケーターに対する延長した逆行を避けます。

## パラメーター
- `RsiPeriod` – RSI計算期間。
- `RsiUpperLevel` – RSI買われすぎしきい値。
- `RsiLowerLevel` – RSI売られすぎしきい値。
- `MaPeriod` – トレンド移動平均の期間。
- `StochKPeriod` – Stochasticオシレーターの%K期間。
- `StochDPeriod` – Stochasticオシレーターの%Dスムーシング期間。
- `StochUpperLevel` – Stochasticの買われすぎレベル。
- `StochLowerLevel` – Stochasticの売られすぎレベル。
- `Volume` – 注文数量。
- `CandleType` – 計算に使用するローソク足データタイプ。

## インジケーター
- 単純移動平均
- 相対力指数
- Stochasticオシレーター

## 取引ルール
- **買い**: 価格がSMAを上回り、RSIが`RsiLowerLevel`以下で、両方のStochastic線が`StochLowerLevel`以下のとき。
- **売り**: 価格がSMAを下回り、RSIが`RsiUpperLevel`以上で、両方のStochastic線が`StochUpperLevel`以上のとき。
- **ロングをクローズ**: RSIまたはStochasticが上限レベルを上抜けたとき。
- **ショートをクローズ**: RSIまたはStochasticが下限レベルを下抜けたとき。
