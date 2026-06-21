# 手動TP・SL付きRSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIが売られすぎレベルを上抜け、かつ終値が過去50本のローソク足の最高終値の70%を上回ったときにロングエントリーします。RSIが買われすぎレベルを下抜け、かつ終値が過去50本のローソク足の最安終値の130%を下回ったときにショートエントリーします。ポジションはパーセンテージベースのテイクプロフィットとストップロスで保護されます。

## パラメーター

- **Candle Type** – ローソク足の時間軸。
- **RSI Length** – RSIの期間。
- **Oversold Level** – ロングエントリー用のRSI閾値。
- **Overbought Level** – ショートエントリー用のRSI閾値。
- **Lookback** – 高値/安値計算の期間。
- **Take Profit %** – テイクプロフィットのパーセンテージ。
- **Stop Loss %** – ストップロスのパーセンテージ。
