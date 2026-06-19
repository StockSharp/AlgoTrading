# MACFibo策略
[English](README.md) | [Русский](README_ru.md)

本策略实现MACFibo系统。当5周期EMA与20周期SMA发生交叉后，算法测量交叉柱收盘价（点A）到最近极值（点B）的距离，并构建斐波那契扩展水平。随后按市价开仓，止盈和止损来自这些水平。当快速EMA与中间SMA反向交叉且头寸亏损时，可选择提前平仓。

## 详情

- **入场条件：**
  - **多头：** 5 EMA上穿20 SMA。点B为下跌段的最低点。
  - **空头：** 5 EMA下穿20 SMA。点B为上涨段的最高点。
- **出场条件：**
  - 在161.8%斐波那契水平或最小止盈距离处获利了结。
  - 在38.2%斐波那契水平或最大止损距离处止损。
  - 如果5 EMA与8 SMA反向交叉且头寸亏损，可提前平仓。
- **过滤器：**
  - 仅在设定的起始和结束小时之间交易。
  - 可禁用周一或周五交易。
- **参数：**
  - `FastLength` – 快速EMA周期。
  - `MidLength` – 用于保护性退出的中间SMA周期。
  - `SlowLength` – 用于趋势判断的慢速SMA周期。
  - `MinTakeProfit` – 最小止盈距离。
  - `MaxStopLoss` – 最大止损距离。
  - `StartHour` / `EndHour` – 允许的交易时间段。
  - `FridayTrade` / `MondayTrade` – 是否在这些日子交易。
  - `CloseAtFastMid` – 在fast/mid交叉时关闭亏损头寸。
  - `CandleType` – 计算所用的K线类型。
