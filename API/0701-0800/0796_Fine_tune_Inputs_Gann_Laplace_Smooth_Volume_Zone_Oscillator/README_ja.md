# Gann + Laplace平滑化ボリュームゾーンオシレーター入力微調整戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、指数移動平均で平滑化されたボリュームオシレーターを使用します。
平滑化されたオシレーターが閾値を上回るとロングポジションを開きます。
負の閾値を下回るとショートポジションを開きます。
シグナルが消え、**Close All** が有効な場合、オープンポジションをすべてクローズします。

## パラメーター
- **Fast Volume EMA** – 高速ボリューム平均の期間。
- **Slow Volume EMA** – 低速ボリューム平均の期間。
- **Smooth Length** – オシレーターの平滑化期間。
- **Threshold** – エントリーのシグナルレベル。
- **Close All** – シグナルがない場合にポジションをクローズ。
- **Candle Type** – 計算に使用するローソク足の種類。
