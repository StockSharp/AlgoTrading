# Divergence + EMA + RSI Close Buy Only

## 概述

该策略将 MetaTrader 的“Divergence + ema + rsi close buy only”专家顾问移植到 StockSharp 的高层 API。它以 **5 分钟 K 线**为核心进行交易，并结合 **1 小时**与 **日线**数据确认趋势方向和超卖状态。系统仅做多：入场需要 MACD 柱状图出现看涨背离，同时满足小时级随机指标在低位金叉与日线 EMA 上行的条件。离场由 RSI 超买阈值以及 `StartProtection` 提供的止损/止盈协同完成。

## 交易逻辑

1. **日线 EMA 趋势过滤**
   - 日线 EMA(9) 必须高于 EMA(20)，说明大趋势向上。
   - 最新收盘的 5 分钟 K 线需位于日线 EMA(9) 下方，以便在回撤时寻找做多机会。

2. **小时随机指标确认**
   - 最近一根已完成的 H1 随机指标 %K 必须落在 `StochasticLowerBound`（默认 0）与 `StochasticUpperBound`（默认 40）之间。
   - %K 在上一根 H1 K 线上需自下而上穿越 %D（当前 %K > %D 且上一根 %K ≤ %D）。

3. **MACD 背离触发（5 分钟）**
   - MACD 柱状图（主线减去信号线）至少提升 `MacdThreshold`，同时 5 分钟收盘价低于上一根收盘价，形成看涨背离。

4. **入场执行**
   - 当所有过滤条件满足且不存在多头持仓时，提交市价买单。如果账户意外持有空头，策略会自动放大下单数量以先行对冲再转为多头。

5. **离场规则**
   - 当 5 分钟 RSI 上穿 `RsiExitLevel`（默认 77）时平掉多单。
   - `StopLossPips` 与 `TakeProfitPips` 为正时，`StartProtection` 会把点值转换为价格距离，自动维护止损与止盈。

6. **订单管理**
   - 每次下新单前都会取消所有挂单，避免重复成交。
   - 交易数量默认使用 `TradeVolume`，可在优化时调整。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 主交易周期。 | 5 分钟 |
| `HourTimeFrame` | 随机指标使用的小时周期。 | 1 小时 |
| `DayTimeFrame` | EMA 趋势过滤的日线周期。 | 1 天 |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD 计算参数。 | 6 / 13 / 5 |
| `MacdThreshold` | 柱状图最小增量，用于判断背离。 | 0.0003 |
| `DailyFastPeriod` / `DailySlowPeriod` | 日线 EMA 周期。 | 9 / 20 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | 小时随机指标设置。 | 30 / 5 / 9 |
| `StochasticUpperBound` / `StochasticLowerBound` | %K 接受区间。 | 40 / 0 |
| `RsiPeriod` | 5 分钟 RSI 周期。 | 7 |
| `RsiExitLevel` | RSI 平仓阈值。 | 77 |
| `TradeVolume` | 下单手数。 | 0.01 |
| `StopLossPips` | 止损距离（点），0 表示关闭。 | 100 |
| `TakeProfitPips` | 止盈距离（点），0 表示关闭。 | 200 |

## 说明

- 策略同时订阅三个数据源（主周期、H1、D1），通过 `Bind`/`BindEx` 与各自的指标联动，实现事件驱动的更新。
- 仅在 K 线收盘后处理数据，与原始 MQL 中使用的 `shift` 行为保持一致。
- MACD 背离判定采用上一根 K 线的收盘价与柱状图值，忠实还原 fxDreema 构建的逻辑。
- 止损/止盈交由 `StartProtection` 管理，适用于回测与实时行情，无需额外手动维护保护单。
