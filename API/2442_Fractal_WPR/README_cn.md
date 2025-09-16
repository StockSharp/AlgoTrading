# Fractal WPR 策略

该策略使用 Williams %R 振荡指标，当指标穿越设定的超买和超卖水平时产生交易信号。它改编自一个 MQL5 专家顾问，展示了一种简单的动量反转系统。

## 工作原理

1. 在选定的时间框上计算可配置周期的 Williams %R 指标。
2. 两条水平线定义极值区域：
   - `HighLevel` 表示超买区（默认 −30）。
   - `LowLevel` 表示超卖区（默认 −70）。
3. 当 `Trend` 设为 `Direct` 时：
   - 向下穿越 `LowLevel` 开多单并平掉所有空单。
   - 向上穿越 `HighLevel` 开空单并平掉所有多单。
4. 当 `Trend` 设为 `Against` 时，上述动作相反。
5. 可选参数允许分别启用或禁用多空仓位的开仓和平仓。
6. 使用高级保护 API 应用以跳数表示的止损和止盈距离。

仅处理已完成的 K 线，以避免对盘中噪声做出反应。

## 参数

- `WprPeriod` – Williams %R 计算周期。
- `HighLevel` – 超买区阈值。
- `LowLevel` – 超卖区阈值。
- `Trend` – 交易模式（`Direct` 或 `Against`）。
- `BuyPositionOpen` – 允许开多仓。
- `SellPositionOpen` – 允许开空仓。
- `BuyPositionClose` – 允许平多仓。
- `SellPositionClose` – 允许平空仓。
- `StopLossTicks` – 止损距离（跳）。
- `TakeProfitTicks` – 止盈距离（跳）。
- `CandleType` – 用于分析的 K 线周期。

## 指标

- Williams %R

