# Daily BreakPoint 策略

## 概述
**Daily BreakPoint Strategy** 是将 MetaTrader 5 专家顾问 “Daily BreakPoint”（构建号 19498）迁移到 StockSharp 的版本。策略关注当前价格与当日开盘价之间的距离。当价格偏离开盘价超过可配置阈值，并且上一根 K 线的实体满足设定的范围时，系统会按照 `CloseBySignal` 参数的设置选择顺势建仓或直接反向开仓。

策略同时订阅两类数据：

1. 由 `CandleType` 指定的日内 K 线，用于产生交易信号。
2. 日线数据，用于跟踪最近一个交易日的开盘价。

## 交易逻辑
1. 每当一根日内 K 线收盘时，策略读取最新的日线开盘价，并使用 `BreakPointPips`（通过合约最小价位转换为绝对价格）计算上下突破水平。
2. 最近收盘的 K 线实体必须位于 `[LastBarSizeMinPips, LastBarSizeMaxPips]` 区间内。
3. **看涨条件**
   - K 线收阳 (`Close > Open`)。
   - 收盘价至少高于当日开盘价 `BreakPointPips`。
   - 突破价格（开盘价 + BreakPoint）必须落在 K 线实体内部。
   - `CloseBySignal = false` 时，策略做多；`CloseBySignal = true` 时，先平掉已有多头再开新空头。
4. **看跌条件** 对称：收阴 K 线、收盘价至少低于当日开盘价 `BreakPointPips`，并且突破价落在实体内部。满足条件后，`CloseBySignal = false` 时做空，`CloseBySignal = true` 时先平旧空头再开多头。
5. 所有订单均以市价下单，手数为 `OrderVolume`。仓位是累计的，多次信号可以逐步加仓或反向减仓。

## 风险控制
- **止损 / 止盈**：通过 `StopLossPips` 与 `TakeProfitPips`（单位为点）设置。值为 0 表示关闭该功能。策略使用 K 线最高价和最低价判断是否触发。
- **移动止损**：当 `TrailingStopPips > 0` 时启用。当浮动盈利超过 `TrailingStopPips + TrailingStepPips` 后，将止损价跟随价格移动，保持 `TrailingStopPips` 的距离；`TrailingStepPips` 可避免在震荡行情中过度调整。
- 所有以点为单位的距离都会根据 `PriceStep` 转换为真实价格。对于 3 位或 5 位小数的报价，1 点等于 10 个最小价位，与原始 EA 的处理一致。

## 参数
| 名称 | 说明 |
| --- | --- |
| `OrderVolume` | 每次市价单的基础手数。 |
| `CloseBySignal` | 为 `true` 时出现反向信号会先平仓再开反向单。 |
| `BreakPointPips` | 触发突破所需的开盘价偏离幅度（点）。 |
| `LastBarSizeMinPips` / `LastBarSizeMaxPips` | 信号 K 线实体允许的最小与最大范围。 |
| `TrailingStopPips` | 移动止损距离，0 表示关闭。 |
| `TrailingStepPips` | 每次移动止损前需要的额外盈利。 |
| `StopLossPips` | 固定止损距离，0 表示不使用。 |
| `TakeProfitPips` | 固定止盈距离，0 表示不使用。 |
| `CandleType` | 用于交易逻辑的日内 K 线类型。 |

## 使用提示
- 策略会自动订阅日内与日线数据，请确认数据源支持所需的时间框架。
- 仅在 K 线收盘后评估信号，因此订单在信号 K 线收盘时发送。
- 点值换算以外汇品种的常见报价为基准，如用于其它最小价位不同的品种，请重新评估参数默认值。
