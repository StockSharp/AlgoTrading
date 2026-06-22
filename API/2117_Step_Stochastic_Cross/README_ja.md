# Step Stochastic Cross戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、ATRに基づくカスタムオシレーターであるStep Stochasticインジケーターを使用してリバーサルシグナルを生成します。ユーザーが選択したローソク足の時間軸を購読し、0から100にスケーリングされたFast（速い）とSlow（遅い）のStep Stochastic線を計算します。

## エントリーとエグジットのルール
- **ロングエントリー:** 遅い線が50以上で、速い線が遅い線を上から下にクロス。
- **ショートエントリー:** 遅い線が50未満で、速い線が遅い線を下から上にクロス。
- **ロングエグジット:** 遅い線が50未満で、ロングポジションのクローズが許可されている。
- **ショートエグジット:** 遅い線が50以上で、ショートポジションのクローズが許可されている。

## パラメーター
- `KFast` – 速いチャネルの乗数。
- `KSlow` – 遅いチャネルの乗数。
- `CandleType` – ローソク足の時間軸。
- `AllowBuyOpen`、`AllowSellOpen`、`AllowBuyClose`、`AllowSellClose` – 取引アクションの許可設定。
- `StopLoss`、`TakeProfit` – 価格単位でのオプションの保護レベル。

戦略は指定された場合にストップロスとテイクプロフィットを適用するために`StartProtection`を呼び出します。

`StepStochasticIndicator`はオリジナルのMQL5インジケーターのC#ポートであり、完成した各ローソク足に対して`Fast`と`Slow`の値を生成します。
