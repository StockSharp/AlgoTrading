# RangeBreakout2 策略

## 概述

**RangeBreakout2** 策略是 MetaTrader 顾问程序“RangeBreakout2”的 StockSharp 移植版本。策略会在设定的时间点构建价格区间，一旦买一/卖一价格突破区间就发送一笔市价单。仓位平仓后循环重新开始。实现保留了原策略的所有仓位管理模式（Constant、Linear、Martingale、Fibonacci），以及在亏损后扩大止盈距离的逻辑。

策略仅操作单个标的，并依赖实时最优买卖价。请确保连接器提供最新的订单簿更新，以保证突破检测及时可靠。

## 交易流程

1. **时间表**：在指定的日/小时记录当前 Ask 价格，并围绕该价格计算突破上下限。
2. **区间计算**：原始区间宽度可通过三种方式得到：
   - **ATR**：读取 ATR 指标并乘以 `AtrPercentage`。
   - **Percent**：使用当前 Ask 价格的 `PricePercentage` 百分比。
   - **Fixed**：将 `FixedRangePoints` 乘以品种的最小报价步长。
3. **突破判定**：当策略处于 `Setup` 阶段时持续监听买一/卖一。如果卖一价高于上轨或买一价低于下轨，则立即下单。
4. **入场方式**：`TradeMode` 控制下单方向——`Stop` 表示顺势突破，`Limit` 表示反向挂单，`Random` 会在每次信号时随机选择其中之一。
5. **风控保护**：止盈止损距离基于原始区间。如果上一笔交易亏损且 `RangeMultiplier > 1`，则把止盈距离按该系数放大。
6. **仓位管理**：基础手数来自投资组合的可用资金（`CurrentValue - BlockedValue`），之后根据所选模式调整：
   - **Constant**：始终使用基础手数。
   - **Linear**：亏损后手数按线性方式递增。
   - **Martingale**：亏损后将前一次手数乘以 `LotMultiplier`。
   - **Fibonacci**：亏损后按斐波那契序列放大手数。

每次仓位关闭后，策略重新回到等待状态，直到下一次时间条件满足。

## 参数

| 组别 | 参数 | 说明 | 默认值 |
|------|------|------|--------|
| Schedule | `Periodicity` | 区间构建频率：Weekly / Daily / NonStop。 | `Weekly` |
| Schedule | `Day` | 当 `Periodicity = Weekly` 时的交易日。 | `Monday` |
| Schedule | `Hour` | 构建区间的小时（兼容原脚本：内部实际使用输入值 +1，≥23 时归零）。 | `0` |
| Range | `RangeMode` | 区间宽度算法（ATR / Percent / Fixed）。 | `Atr` |
| Range | `AtrPercentage` | ATR 百分比系数。 | `50` |
| Range | `AtrLength` | ATR 指标长度。 | `20` |
| Range | `PricePercentage` | `Percent` 模式下使用的价格百分比。 | `1` |
| Range | `FixedRangePoints` | `Fixed` 模式下的点数。 | `1000` |
| Trading | `RangePercentage` | 上下轨与原始区间的比例。 | `100` |
| Trading | `TradeMode` | 入场方式：顺势/逆势/随机。 | `Stop` |
| Trading | `TakeProfitPercentage` | 止盈距离占区间的比例。 | `100` |
| Trading | `StopLossPercentage` | 止损距离占原始区间的比例。 | `100` |
| Risk | `LotMode` | 仓位管理模式（Constant / Linear / Martingale / Fibonacci）。 | `Martingale` |
| Risk | `MarginPercentage` | 基础手数占自由资金的百分比。 | `10` |
| Risk | `LotMultiplier` | 马丁格尔类模式的乘数。 | `2` |
| Risk | `RangeMultiplier` | 亏损后用于放大止盈的系数。 | `1` |
| Data | `SignalCandleType` | 用于驱动时间表的蜡烛类型。 | `1 分钟` |
| Data | `AtrCandleType` | ATR 使用的蜡烛类型，仅在 `RangeMode = Atr` 时订阅。 | `1 天` |

## 实现细节

- 策略依赖实时最优买卖价；若连接器不提供相关数据，则无法检测突破。
- 若投资组合缺少 `CurrentValue` 或 `BlockedValue`，基础手数会降级为交易所允许的最小数量。
- 使用 `SetStopLoss` 与 `SetTakeProfit` 注册保护订单，并传入进场后的结果仓位以便统一管理。
- 当 ATR 尚未形成时，会退回到“当前 Ask × 1%”的默认区间，与原始 EA 行为一致。
- `Random` 模式使用 .NET 随机数生成器，在不同的突破事件中可能产生不同的入场方向。

## 使用建议

1. 根据所需粒度调整 `SignalCandleType`。一分钟蜡烛能最大程度还原原脚本的逐笔触发方式。
2. Weekly/Daily 模式依赖服务器时间，请确认时区设置与原策略一致。
3. 当使用马丁格尔类仓位管理时，提高 `RangeMultiplier` 会显著增加连续亏损时的风险敞口。
4. `RangePercentage` 越大，止盈与止损的距离也会同步放大，请结合品种波动性进行设置。
