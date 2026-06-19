# Simple FX 策略

## 概述
该策略使用两条指数移动平均线来检测趋势变化。短期 EMA 上穿长期 EMA 时做多，短期 EMA 下穿长期 EMA 时做空。

## 参数
- **Long MA Period** – 长期 EMA 的周期。
- **Short MA Period** – 短期 EMA 的周期。
- **Stop Loss (points)** – 以价格跳动点数表示的止损。
- **Take Profit (points)** – 以价格跳动点数表示的止盈。
- **Candle Type** – 使用的K线时间框架。
