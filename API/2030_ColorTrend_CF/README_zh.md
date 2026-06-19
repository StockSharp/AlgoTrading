# Color Trend CF 策略

该策略是 MQL 专家 **Exp_ColorTrend_CF** 的移植版本。策略使用两条指数移动平均线来识别趋势变化。快速 EMA 对价格变化反应灵敏，慢速 EMA 用于过滤趋势。当快速 EMA 上穿慢速 EMA 时开多仓；当快速 EMA 下穿慢速 EMA 时开空仓。

## 参数

- `Period` – 快速 EMA 的基础周期，慢速 EMA 使用其两倍。
- `StopLoss` – 以价格单位表示的止损距离。
- `TakeProfit` – 以价格单位表示的止盈距离。
- `AllowBuyOpen` – 允许开多仓。
- `AllowSellOpen` – 允许开空仓。
- `AllowBuyClose` – 允许在卖出信号时平多仓。
- `AllowSellClose` – 允许在买入信号时平空仓。
- `CandleType` – 指标计算使用的时间框架。

## 交易逻辑

1. 订阅所选时间框架的K线。
2. 计算快慢 EMA。
3. 当快速 EMA 上穿慢速 EMA 时：
   - 如允许则平空仓。
   - 如允许则开多仓。
4. 当快速 EMA 下穿慢速 EMA 时：
   - 如允许则平多仓。
   - 如允许则开空仓。
5. 对已有仓位应用止损和止盈。

该实现使用 StockSharp 的高级 API 和指标绑定功能。
