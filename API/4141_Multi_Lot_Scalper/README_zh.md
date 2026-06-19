# Multi Lot Scalper 策略

## 概述

**Multi Lot Scalper** 策略源自 MetaTrader 平台的同名专家顾问。它依靠 MACD 直方图的斜率判断多空方向，并在方向确立后通过逐步加仓的方式构建网格。每当价格逆向运行到达既定的点差，策略就会以更大的手数继续加仓，从而以“金字塔”方式降低平均持仓成本。移植到 StockSharp 后，策略保留了原始的入场逻辑、资金管理与保护措施，同时利用高级别的蜡烛图订阅接口完成指标计算和信号处理。

默认情况下策略订阅 15 分钟蜡烛，但可以通过 `CandleType` 参数切换到任何适合目标品种的周期。

## 交易逻辑

1. **信号识别**：在每根完成的蜡烛上计算 MACD 指标（`MacdFastLength`、`MacdSlowLength`、`MacdSignalLength`）。当 MACD 主线较上一根上升时判定为多头信号，下降则判定为空头信号。`ReverseSignals` 参数可用于反向解读。
2. **首次建仓**：只要日期和时间过滤器（`StartYear`、`StartMonth`、`EndYear`、`EndMonth`、`EndHour`、`EndMinute`）允许，策略会在出现信号后立即以市价单建仓，与 MQL 实现保持一致。
3. **分批加仓**：只有当价格相对最后一笔成交逆向移动至少 `EntryDistancePips` 点时才会增加新的仓位。每一笔额外交易会把基础手数乘以 2，或在 `MaxTrades` 大于 12 时乘以 1.5，从而复刻原策略的马丁加仓节奏。
4. **止损与止盈**：`InitialStopPips` 与 `TakeProfitPips` 会换算成整篮仓位的止损、止盈。若盈利幅度超过 `EntryDistancePips + TrailingStopPips`，则激活追踪止损，将保护价格推向有利方向。
5. **账户保护**：当持仓数接近上限（`MaxTrades - OrdersToProtect`）且浮动收益达到 `SecureProfit` 时，如果开启 `UseAccountProtection`，策略会关闭最后一笔交易并暂时禁止继续加仓。

## 资金管理

原版 EA 可以根据账户余额动态调整基础手数。StockSharp 版本通过 `UseMoneyManagement`、`RiskPercent`、`IsStandardAccount` 三个参数保留了该功能。当启用时，`LotSize` 将被忽略，策略会根据组合价值 (`Portfolio.CurrentValue`) 计算新的基准手数，并按标准账户或迷你账户规则进行缩放。

## 参数

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `TakeProfitPips` | 每笔订单的止盈距离（点）。 | `40` |
| `LotSize` | 禁用资金管理时使用的基础手数。 | `0.1` |
| `InitialStopPips` | 初始止损距离（点）。 | `0` |
| `TrailingStopPips` | 追踪止损距离。 | `20` |
| `MaxTrades` | 同时允许的最大加仓次数。 | `10` |
| `EntryDistancePips` | 触发下一次加仓所需的逆向点差。 | `15` |
| `SecureProfit` | 触发账户保护所需的浮动利润（账户货币）。 | `10` |
| `UseAccountProtection` | 是否在达到安全利润后关闭最新仓位。 | `true` |
| `OrdersToProtect` | 受安全利润规则约束的尾部订单数量。 | `3` |
| `ReverseSignals` | 是否反转 MACD 信号方向。 | `false` |
| `UseMoneyManagement` | 是否启用余额驱动的手数计算。 | `false` |
| `RiskPercent` | 启用资金管理时的风险系数。 | `12` |
| `IsStandardAccount` | 是否按标准账户（1 手 = 100000）计算。 | `false` |
| `EurUsdPipValue` | EURUSD 的单点价值。 | `10` |
| `GbpUsdPipValue` | GBPUSD 的单点价值。 | `10` |
| `UsdChfPipValue` | USDCHF 的单点价值。 | `10` |
| `UsdJpyPipValue` | USDJPY 的单点价值。 | `9.715` |
| `DefaultPipValue` | 其他品种的默认单点价值。 | `5` |
| `StartYear` | 允许新建仓位的首个年份。 | `2005` |
| `StartMonth` | 允许新建仓位的首个月份。 | `1` |
| `EndYear` | 限制新建仓位的最后年份。 | `2006` |
| `EndMonth` | 限制新建仓位的最后月份。 | `12` |
| `EndHour` | 每日停止加仓的小时（24 小时制）。 | `22` |
| `EndMinute` | 每日停止加仓的分钟。 | `30` |
| `CandleType` | 用于信号计算的蜡烛类型（默认 15 分钟）。 | `15 分钟` |
| `MacdFastLength` | MACD 快速均线周期。 | `14` |
| `MacdSlowLength` | MACD 慢速均线周期。 | `26` |
| `MacdSignalLength` | MACD 信号线周期。 | `9` |

## 使用建议

- 请确认交易品种的最小报价单位与策略假设的点值一致，如有差异需调整相关参数。
- 马丁加仓会迅速放大总持仓，请在实盘前使用较保守的 `MaxTrades`、`EntryDistancePips` 与 `TrailingStopPips` 组合进行回测。
- 针对不同品种优化 MACD 参数与时间框架：较慢的周期减少加仓次数，较快的周期增加交易频率。
- 如果账户保护过于频繁地触发，可降低 `SecureProfit` 或缩短 `TrailingStopPips`。
- 时间过滤器非常适合规避重大新闻或低流动性时段，可根据需求调整 `EndHour` 与 `EndMinute`。

## 转换说明

- 该实现通过 `SubscribeCandles().BindEx(...)` 获取蜡烛与指标数据，避免手动管理指标缓存。
- 追踪止损针对整体持仓设置保护价位，而非逐单修改，适合 StockSharp 的组合化仓位模型。
- MQL 中基于 `AccountBalance` 的手数算法对应为 `Portfolio.CurrentValue`，以维持原策略的风险控制逻辑。
