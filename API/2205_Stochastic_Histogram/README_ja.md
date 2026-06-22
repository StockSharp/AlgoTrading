# Stochasticヒストグラム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はオリジナルのMQLエキスパート `Exp_Stochastic_Histogram` のStockSharp移植版です。
Stochasticオシレーターを使用して2つのモードで逆張りの取引シグナルを生成します:

- **Levels** – %Kが `HighLevel` と `LowLevel` で定義された買われすぎまたは売られすぎのエリアを出たときにシグナルが現れます。
- **Cross** – %Kが%Dラインを交差したときにシグナルが現れます。取引はクロスオーバーの反対方向に開かれます。

新しいシグナルを受け取るたびに、戦略は既存のポジションを閉じて必要な方向に新しいポジションを開きます。

## パラメーター

- `KPeriod` – メイン%K期間。
- `DPeriod` – %Dスムージング期間。
- `Slowing` – %Kの追加スムージング。
- `HighLevel` – Levelsモードの上限閾値。
- `LowLevel` – Levelsモードの下限閾値。
- `Mode` – LevelsまたはCross。
- `CandleType` – 計算に使用するローソク足の時間軸。

## 動作方法

完成した各ローソク足に対して、Stochasticオシレーターが更新・評価されます。**Levels**モードでは%Kが高いレベルを下回って戻ったときにロングトレードが開かれ、%Kが低いレベルを上回って戻ったときにショートトレードが開かれます。**Cross**モードでは%Kが%Dを下回る下方クロスオーバーでロングトレードが開かれ、上方クロスオーバーはショートトレードを発動させます。戦略は常に最大1つのオープンポジションのみを保有します。
