# Omzdwwi Pending Manager 策略

## 概述

**Omzdwwi Pending Manager** 是 MetaTrader 4 顾问 `omzdwwi7739cyjayvs_1_65.mq4` 的 StockSharp 高层 API 复刻版。原脚本以“挂单矩阵”为核心：在当前价附近维持多种挂单、在指定时间触发市价单，并对持仓与挂单执行跟踪止损。本策略完全依赖 `Strategy` 的高级接口（`SubscribeLevel1`、`BuyStop`、`ReRegisterOrder` 等），不需要直接操作底层连接器。

主要功能：

- 在 Bid/Ask 两侧维持多达四张挂单（buy stop、sell stop、buy limit、sell limit），距离可调。
- 到达 `SignalHour:SignalMinute` 时，根据 `Time*Signal` 参数触发市价买入或卖出，同时支持限制 `WaitClose` / `MaxMarketOrders`。
- 对持仓应用固定止盈/止损、额外的 `ExitProfitPoints` 目标，以及与 MQL 中 `TrailingPositions()` 相同的跟踪止损逻辑。
- 对挂单执行 `TrailingOtlozh()` 的“拖动”规则：当价格超过 `offset + step`，自动重新挂单使其贴近行情。
- 监控账户层面的收益/回撤百分比，达到阈值时通过日志提示，等效于 MT4 顾问的提示框。

## 数据订阅与信号触发

- 使用 `SubscribeLevel1()` 获取最优买卖价。每次报价更新都会执行时间检查、挂单管理、跟踪调整以及退出判断。
- `GetWorkingSecurities()` 仅申明 Level1 订阅，便于在实盘或回测环境中运行。

## 入场逻辑

1. **定时市价单。** 当服务器时间达到 `SignalHour:SignalMinute` 且 `UseTimeSignals=true` 时，会设置内部标志位（例如 `TimeBuySignal`）。在随后到来的报价更新中，如果仓位限制允许，则调用 `BuyMarket()` 或 `SellMarket()` 并立即清除标志。
2. **持续挂单。** 对于开启的挂单类型，策略检查是否已存在活动订单。若没有，则按 `StepPoints * PriceStep` 的距离重新下单；若已有，则比较价格并通过 `ReRegisterOrder` 调整，使逻辑与原顾问的 `UstanOtlozh()` 保持一致。

## 持仓退出

- **固定止盈/止损** 由 `MarketTakeProfitPoints` 与 `MarketStopLossPoints` 定义，一旦 Bid/Ask 触及相应价格，立即用市价单平仓。
- **额外利润目标 `ExitProfitPoints`** 等同于 MQL 的 `PipsProfit` 参数，即使没有设置止盈也会在达到该利润后平仓。
- **跟踪止损** 复制了 `TrailingPositions()`：当浮动盈利满足条件（或 `RequireProfitBeforeTrailing=false`），多头的内部止损会更新为 `Bid - offset`，空头更新为 `Ask + offset`，并保证最小移动 `MarketTrailingStepPoints`。

## 挂单跟踪

- Stop 型挂单使用 `StopTrailingOffsetPoints` 与 `StopTrailingStepPoints`。当 Ask/Bid 超过阈值后，调用 `ReRegisterOrder` 将挂单移动到新的 `Ask + offset` 或 `Bid - offset`。
- Limit 型挂单对应 `LimitTrailingOffsetPoints` 与 `LimitTrailingStepPoints`，逻辑同上。

## 账户与风险控制

- `MaxMarketOrders` 在 `WaitClose=false` 时限制同方向累计的手数（按 `OrderVolume` 的倍数计算）。
- `UseGlobalLevels`、`GlobalTakeProfitPercent`、`GlobalStopLossPercent` 监控投资组合的权益变动并输出提示。
- `SlippagePoints` 保留为兼容参数，不影响逻辑。

## 参数列表

| 组别 | 参数 | 说明 |
|------|------|------|
| 通用 | `OrderVolume` | 每次下单使用的手数。 |
| 执行 | `WaitClose` | 是否必须在仓位归零后才允许再次入场。 |
| 执行 | `MaxMarketOrders` | 在允许加仓时的最大手数限制。 |
| 挂单 | `EnableBuyStop` / `EnableSellStop` / `EnableBuyLimit` / `EnableSellLimit` | 控制每种挂单是否启用。 |
| 挂单 | `StopStepPoints`, `LimitStepPoints` | 挂单与当前价之间的距离（点）。 |
| 挂单 | `StopTakeProfitPoints`, `StopStopLossPoints`, `LimitTakeProfitPoints`, `LimitStopLossPoints` | 挂单成交后的虚拟止盈/止损距离。 |
| 挂单 | `StopTrailingOffsetPoints`, `StopTrailingStepPoints`, `LimitTrailingOffsetPoints`, `LimitTrailingStepPoints` | 挂单的跟踪参数。 |
| 持仓 | `MarketTakeProfitPoints`, `MarketStopLossPoints` | 市价单的止盈、止损点数。 |
| 持仓 | `MarketTrailingOffsetPoints`, `MarketTrailingStepPoints`, `RequireProfitBeforeTrailing` | 跟踪止损配置。 |
| 持仓 | `ExitProfitPoints` | 额外利润目标。 |
| 时间 | `UseTimeSignals`, `SignalHour`, `SignalMinute` | 定时开仓配置。 |
| 时间 | `TimeBuySignal`, `TimeSellSignal`, `TimeBuyStopSignal`, `TimeSellStopSignal`, `TimeBuyLimitSignal`, `TimeSellLimitSignal` | 定时触发的订单类型。 |
| 账户 | `UseGlobalLevels`, `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | 权益监控阈值。 |
| 其他 | `SlippagePoints` | 兼容性参数，保留未用。 |

## 转换说明

- MT4 中的挂单直接带有止盈/止损；在 StockSharp 中，策略通过监控价格并用市价单平仓来达到相同效果。
- 声音提示被 `AddInfoLog` / `AddWarningLog` 日志替代。
- MT4 的 `MODE_STOPLEVEL` 无法在 StockSharp 中获取，需由使用者自行确保下单距离符合交易所要求。

## 使用步骤

1. 选择具有有效 `PriceStep` 的 `Security` 与 `Portfolio`。
2. 根据需要设置各参数（点值自动通过 `Security.ShrinkPrice` 转换为价格）。
3. 启动策略，系统会订阅报价并立即按照原顾问的思路管理仓位与挂单。

> **提示：** 回测时务必提供 Level1 报价数据，以便定时器与跟踪逻辑在每个行情更新上执行，与 MT4 行为保持一致。
