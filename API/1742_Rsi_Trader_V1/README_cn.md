# RSI Trader V1 策略
[English](README.md) | [Русский](README_ru.md)

该策略利用相对强弱指数（RSI）在短期极值后寻找反转。当 RSI 连续两根 K 线位于超卖区以下后向上突破 `BuyPoint` 时开多；当 RSI 连续两根 K 线位于超买区以上后向下跌破 `SellPoint` 时开空。策略可选在出现反向信号时关闭已有仓位，并且只在指定的时间区间内交易。

## 细节

- **入场条件**：
  - **多头**：`RSI > BuyPoint` 且前两根 K 线的 RSI 均 `< BuyPoint`。
  - **空头**：`RSI < SellPoint` 且前两根 K 线的 RSI 均 `> SellPoint`。
- **出场条件**：反向信号或固定的止盈/止损。
- **时间过滤**：只有当 K 线开盘时间的小时数在 `StartHour` 与 `EndHour` 之间时才允许交易。
- **止盈止损**：以价格单位表示的固定止盈和止损。
- **参数**：
  - `RsiPeriod` – RSI 计算周期。
  - `BuyPoint` – 做多触发的超卖阈值。
  - `SellPoint` – 做空触发的超买阈值。
  - `CloseOnOpposite` – 出现反向信号时是否平掉当前仓位。
  - `StartHour` / `EndHour` – 允许交易的小时范围。
  - `TakeProfit` / `StopLoss` – 价格单位的止盈与止损。

该示例展示了如何使用 StockSharp 高级 API 构建一个简洁的 RSI 反转策略，可作为进一步研究的基础。
