# Anchored Momentumローソク足戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMQL5のエキスパート「AnchoredMomentumCandle」をStockSharp C#サンプルに変換したものです。指数移動平均と単純移動平均を使用して、ローソク足の始値と終値のアンカードモメンタムを計算します。インジケーターはモメンタムの方向を反映した色の合成ローソク足を描画します。

**青**のローソク足への変化でロングポジションを開き、ショートポジションを閉じます。**ピンク**のローソク足への変化でショートポジションを開き、ロングポジションを閉じます。

## パラメーター
- **Momentum Period** – 単純移動平均の長さ。
- **Smooth Period** – 指数移動平均の長さ。
- **Candle Type** – 計算に使用するローソク足の時間軸。

戦略は指定されたローソク足を購読し、インジケーターを計算し、色の変化時に成行注文を発注します。
