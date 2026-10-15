# シンプル FX 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は2本の指数移動平均を使用してトレンドの変化を検出します。短期EMAが長期EMAを上抜けるとロングポジションが開き、短期EMAが長期EMAを下抜けるとショートポジションが開きます。

## パラメーター
- **Long MA Period** – 長期EMAの期間。
- **Short MA Period** – 短期EMAの期間。
- **Stop Loss (points)** – 価格ステップでの防護ストップ。
- **Take Profit (points)** – 価格ステップでの利益目標。
- **Candle Type** – ローソク足の時間軸。
