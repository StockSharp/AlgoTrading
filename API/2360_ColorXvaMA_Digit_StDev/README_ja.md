# ColorXvaMA Digit StDev戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は価格が指数移動平均（EMA）からどれだけ乖離しているかに基づいて取引します。2つの乖離乗数（K1とK2）が、価格の標準偏差から計算された内側と外側のバンドを定義します。

価格がEMAよりK2標準偏差以上上昇すると、戦略はロングポジションに入ります。価格がEMAよりK2標準偏差以上下落すると、ショートポジションに入ります。乖離がK1で定義された内側のバンドに戻ると、既存のポジションはクローズされます。

## パラメーター
- **EMA Length** – 指数移動平均の期間。
- **StdDev Length** – 標準偏差計算の期間。
- **Deviation K1** – ポジションを閉じるために使用する内側バンドの乗数。
- **Deviation K2** – ポジションを開くために使用する外側バンドの乗数。
- **Candle Type** – ローソク足の時間軸。

## インジケーター
- Exponential Moving Average
- StandardDeviation

## 動作原理
1. 選択した時間軸のローソク足を購読する。
2. EMAと価格の標準偏差を計算する。
3. EMAからの価格乖離を計算する。
4. 乖離が±K2×StdDevを超えるとロング/ショートに参入する。
5. 乖離が±K1×StdDevの範囲内に戻るとエグジットする。

このアプローチは強い平均乖離を捉え、反転時にエグジットすることを目指しています。
