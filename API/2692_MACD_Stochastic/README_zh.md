# 2692 MACD Stochastic Strategy

## 概述
该策略是对 MetaTrader 5 指标策略 “MACD Stochastic” 的 StockSharp 实现。交易逻辑利用 MACD 金叉/死叉，并可选地要求随机指标的确认，同时只在三个可配置的日内交易时段内执行交易。每笔仓位都按点数设定初始止损和止盈，并提供可选的追踪止损，以便在达到指定利润后将止损推至保本区。

## 指标
- **MACD**：监控快线和慢线与信号线的交叉，用作主要的趋势反转信号来源。
- **随机指标（Stochastic）**：可选过滤器，通过检查 %K 与 %D 的最新交叉是否支持交易方向来确认 MACD 信号。

## 交易逻辑
### 做多条件
1. MACD 主线向上穿越信号线，且两条线均位于零轴下方。
2. 当前柱上尚未开仓（每根 K 线最多开一次仓位）。
3. 当前本地时间处于任意一个启用的交易时段内。
4. 启用随机指标过滤时，当前 %K 必须高于 %D，并且 *StochasticBarsToCheck* 根 K 线之前的值应表现出相反关系，表明出现新的看涨交叉。

### 做空条件
1. MACD 主线向下穿越信号线，且两条线都在零轴上方。
2. 没有持仓，并且本柱尚未触发过交易。
3. 当前时间位于有效交易时段内。
4. 启用随机指标过滤时，当前 %K 必须低于 %D，且历史值显示相反关系，从而确认看跌交叉。

### 仓位管理
- **止损 / 止盈**：按点数计算，基于品种的 `PriceStep`。对于三位或五位小数报价，程序会将步长乘以 10 来接近标准点值。
- **追踪止损**：当浮动盈利达到 *WhenSetNoLossStopPips* 点后开始生效：
  - 多单需要存在初始止损。只要新的止损保持至少 *TrailingStepPips + TrailingStopPips* 点的安全距离并高于 *NoLossStopPips* 定义的保本线，就会按 *TrailingStopPips* 点提升止损。
  - 空单在类似约束下下移止损；若没有初始止损，可在价格走出足够利润后于 *NoLossStopPips* 处放置保本止损。
- **止损/止盈触发**：当蜡烛的最高价或最低价触及保存的退出价位时，仓位将以市价平仓，并清空内部状态。

## 参数
- **MacdFastPeriod, MacdSlowPeriod, MacdSignalPeriod**：MACD 参数。
- **UseStochastic**：是否启用随机指标确认。
- **StochasticBarsToCheck, StochasticLength, StochasticKPeriod, StochasticDPeriod**：随机指标配置。
- **Volume**：每次交易的手数。
- **StopLossPips, TakeProfitPips**：初始止损和止盈的点数。
- **TrailingStopPips, TrailingStepPips**：追踪止损设置。
- **NoLossStopPips, WhenSetNoLossStopPips**：保本止损与激活阈值。
- **MaxPositions**：为兼容原策略而保留；由于 StockSharp 使用净头寸模式，策略始终只持有单一方向的净仓位。
- **Session1/2/3 Start-End**：允许交易的时间段。若要禁用某个时段，可将开始和结束都设为 `00:00`。
- **CandleType**：用于生成信号的 K 线类型。

## 其他说明
- 所有决策均在收盘后执行，且每根 K 线只允许一次入场，与原始 EA 行为一致。
- 点值计算依赖于品种的价格步长，请确保 `PriceStep` 数据可用。
- 随机指标过滤器通过维护小型历史队列来回溯检查，无需访问底层指标缓冲区，符合高层 API 的最佳实践。
