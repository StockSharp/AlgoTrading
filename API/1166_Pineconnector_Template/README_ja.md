# Pineconnector戦略テンプレート
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、任意のインジケーターを接続してトレードシグナルを生成する方法を示します。例として2本の移動平均線を使用し、速い平均が遅い平均を上抜けたときにロングエントリー、逆のクロスでショートエントリーします。

## パラメーター
- **Fast Length** – 速い移動平均線の期間。
- **Slow Length** – 遅い移動平均線の期間。
- **Candle Type** – 計算に使用するローソク足の種類。
