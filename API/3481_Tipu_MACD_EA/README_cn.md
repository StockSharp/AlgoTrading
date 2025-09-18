# Tipu MACD EA 策略

## 概述
该策略是将 MQL4 上的 **Tipu MACD EA** 迁移到 StockSharp 的高层 API 实现。策略在单一品种上运行，依靠 MACD 指标给出的信号，并保留了原始专家顾问的主要功能：

* 带有两个时间段的交易时间过滤器。
* 支持零轴和信号线交叉的 MACD 入场信号，可配置 EMA 周期与读取的位移。
* 自动化持仓管理：止盈、止损、移动止损以及无损保护。
* “最大手数”限制，对应 MQL 版本中的 `dMaxLots` 设置。

所有交易均使用市价单执行。策略在内部跟踪保护价位，只要某根完成的蜡烛触碰到止损或止盈，就立即平仓。

## 交易流程
1. 订阅配置好的蜡烛类型，并计算 `MovingAverageConvergenceDivergenceSignal` 指标（MACD 线与信号线）。
2. 按照 `MacdShift` 参数读取 MACD 数值（0 表示当前蜡烛，1 表示上一根蜡烛），并生成两类交叉信号：
   * **零轴交叉**（可选）——MACD 上穿 0 时做多，下穿 0 时做空。
   * **信号线交叉**（可选）——MACD 上穿信号线时做多，下穿时做空。
3. 若启用了交易时间过滤器，则只有当当前小时落在任一时间段内才允许开仓。
4. 当出现做多信号时：
   * 如果不允许对冲且当前持有空头，根据 `CloseOnReverseSignal` 的设定选择先平空或放弃本次入场。
   * 以 `TradeVolume` 与剩余可用头寸上限 `MaxPositionVolume` 中的较小值下达买入市价单。
   * 更新多头平均入场价，并根据配置生成止盈止损。
5. 当出现做空信号时执行对称操作。
6. 持仓期间：
   * 每根完成的蜡烛都会检查止损或止盈是否被触及，满足条件立即平仓。
   * 启用移动止损后，当浮盈达到 `TrailingPips + TrailingCushionPips` 时，将止损上调/下调至距离当前价 `TrailingPips` 的位置。
   * 启用无损保护后，当利润超过 `RiskFreePips` 时，把止损移动到入场价。

## 参数说明
| 名称 | 说明 |
| --- | --- |
| `CandleType` | 用于计算 MACD 的蜡烛类型。 |
| `TradeVolume` | 每次市价单的下单手数。 |
| `MaxPositionVolume` | 多头或空头的最大累计仓位。 |
| `UseTimeFilter` | 是否启用交易时间过滤。 |
| `Zone1StartHour`, `Zone1EndHour` | 第一个时间段的起止小时（包含端点，交易所时区）。 |
| `Zone2StartHour`, `Zone2EndHour` | 第二个时间段的起止小时。 |
| `FastPeriod`, `SlowPeriod`, `SignalPeriod` | MACD 的快 EMA、慢 EMA 与信号线长度。 |
| `MacdShift` | 0=使用当前柱，1=使用上一柱（对应 MQL 的 `iShift`）。 |
| `UseZeroCross` | 是否启用零轴交叉信号。 |
| `UseSignalCross` | 是否启用 MACD 与信号线交叉信号。 |
| `AllowHedging` | 是否允许在不平仓的情况下反向建仓。 |
| `CloseOnReverseSignal` | 当出现反向信号时是否先平掉相反仓位（在禁止对冲时使用）。 |
| `UseTakeProfit`, `TakeProfitPips` | 是否启用止盈以及止盈的点数。 |
| `UseStopLoss`, `StopLossPips` | 是否启用止损以及止损的点数。 |
| `UseTrailingStop`, `TrailingPips`, `TrailingCushionPips` | 移动止损的距离与额外缓冲（点数）。 |
| `UseRiskFree`, `RiskFreePips` | 盈利达到指定点数后将止损移动到入场价。 |

## 使用建议
* 将蜡烛类型设置为与 MetaTrader 中使用的周期一致（默认 15 分钟）。
* 策略通过 `Security.PriceStep` 推导点值，若该属性缺失则使用默认的 0.0001。
* 策略假设市价单即时成交；在实盘中如有需要应自行考虑滑点控制。
* 如果同时关闭零轴和信号线信号，策略将保持空仓状态。
