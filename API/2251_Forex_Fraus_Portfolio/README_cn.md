# Forex Fraus Portfolio 策略

该策略基于长周期的 **Williams %R** 指标，只交易单一品种。当指标离开极端区间时，策略按照突破方向开仓。

## 工作原理

1. 计算 `WprPeriod` 根K线的 Williams %R。
2. 当指标跌破 `BuyThreshold` 时，准备做多；随后指标回到阈值之上时，以市价买入。
3. 当指标升破 `SellThreshold` 时，准备做空；随后指标跌回阈值之下时，以市价卖出。
4. 只在 `StartHour` 与 `StopHour` 之间的时间窗口内交易。
5. 可通过参数启用止损、止盈和追踪止损。

## 参数

- `WprPeriod` – Williams %R 周期。
- `BuyThreshold` – 做多信号阈值。
- `SellThreshold` – 做空信号阈值。
- `StartHour` / `StopHour` – 交易时段。
- `SlPoints` – 止损点数，0 表示禁用。
- `TpPoints` – 止盈点数，0 表示禁用。
- `UseTrailing` – 是否使用追踪止损。
- `TrailingStop` – 追踪止损距离（点）。
- `TrailingStep` – 追踪止损更新步长（点）。
- `CandleType` – 订阅的K线类型。

## 备注

原始的 MQL4 版本针对多个货币对进行交易，并分别管理每个订单。本 C# 版本专注于单一品种，演示如何使用 StockSharp 的高级 API 实现核心逻辑。
