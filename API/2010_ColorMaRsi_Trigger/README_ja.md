# ColorMaRsiトリガー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、オリジナルのMQL5エキスパート `exp_colormarsi-trigger.mq5` のStockSharpポートです。高速・低速のEMAと高速・低速のRSI値を比較します。組み合わせシグナルは -1、0、または +1 の値を取ります。前のシグナルが現在のシグナルと逆符号の場合にポジションが開かれます。

## 動作方法

- シグナルが正からゼロまたは負に変わると、ロングポジションが開かれ、ショートポジションが閉じられます。
- シグナルが負からゼロまたは正に変わると、ショートポジションが開かれ、ロングポジションが閉じられます。

## パラメーター

- **Fast EMA** – 高速指数移動平均の期間。
- **Slow EMA** – 低速指数移動平均の期間。
- **Fast RSI** – 高速RSIの期間。
- **Slow RSI** – 低速RSIの期間。
- **Candle Type** – 計算に使用するローソク足の時間軸。

## インジケーター

- 指数移動平均（高速と低速）
- 相対力指数（高速と低速）

完成したローソク足のみが処理されます。注文は `BuyMarket` と `SellMarket` を使って発注されます。
