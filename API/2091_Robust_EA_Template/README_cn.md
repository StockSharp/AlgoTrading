# Robust EA 模板策略

该策略根据 MQL 的 Robust EA 模板实现，使用 CCI 和 RSI 指标生成交易信号，并应用固定的止盈与止损。

## 逻辑
- 当 CCI 位于 -200..-150 或 -100..-50 且 RSI 在 0 到 25 之间时买入。
- 当 CCI 位于 50 到 150 且 RSI 在 80 到 100 之间时卖出。
- 止损和止盈以点数表示，并转换为价格。

## 参数
- `Candle Type` – 蜡烛图类型。
- `CCI Period` – CCI 指标周期。
- `RSI Period` – RSI 指标周期。
- `Take Profit (pips)` – 止盈距离。
- `Stop Loss (pips)` – 止损距离。
- `Volume` – 交易量。
