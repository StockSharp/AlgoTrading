# CashMachine 策略

## 概述
CashMachine 策略复刻自原始的 MQL4 智能交易系统，该系统通过 EURUSD 与 USDCHF 组成的对冲组合进行交易。当满足以下三个条件时，会同时对基准品种（策略的 `Security`）和对冲品种开仓：

1. 基准品种的快 EMA 相对慢 EMA 出现向上或向下突破。
2. 基准品种的 RSI 指标进入超卖（<= 30）或超买（>= 70）区间。
3. 基准品种与对冲品种的日线偏差（`Close - SMA`）之间的皮尔逊相关系数为负值，表明两者的走势应再次趋同。

策略始终以相同的手数对两腿建仓，并在两腿的浮动盈利达到 `TakeProfit` 参数时立即平仓。

## 策略流程
1. **指标准备**
   - 对两个品种订阅 `CandleType` 指定的盘中周期，在基准品种上计算快/慢 EMA 以及 RSI。
   - 对两个品种同时订阅 `DailyCandleType` 指定的日线周期，并用长度为 `CorrelationLookback` 的简单均线平滑日收盘价。
   - 计算日线偏差 `Close - SMA`，仅保留最近 `CorrelationLookback` 组偏差用于计算皮尔逊相关系数。
2. **交易时间窗口**
   - 完全复现原版 EA 的限制：仅允许在每个月的前 5 个日历日交易，并且在第 5 天的 18:00 之后停止开新仓。
3. **开仓条件**
   - 两个品种均无持仓。
   - 相关系数为负值，且已经累计 `CorrelationLookback` 组日线偏差。
   - 多头：`fast EMA > slow EMA` 且 `RSI <= RsiOversold`，两腿同时市价买入。
   - 空头：`fast EMA < slow EMA` 且 `RSI >= RsiOverbought`，两腿同时市价卖出。
4. **获利了结**
   - 每根完成的 K 线都会刷新最近收盘价。当两腿的浮动利润达到 `TakeProfit` 时立即平仓。
5. **持仓追踪**
   - 分别为两腿记录平均开仓价与当前仓位，使用最新收盘价计算浮动盈亏。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `TakeProfit` | `decimal` | `10` | 两腿合计的浮动盈利达到该值后全部平仓。 |
| `EmaShortPeriod` | `int` | `8` | 基准品种快 EMA 的周期。 |
| `EmaLongPeriod` | `int` | `21` | 基准品种慢 EMA 的周期。 |
| `RsiPeriod` | `int` | `14` | RSI 的计算周期。 |
| `RsiOversold` | `decimal` | `30` | 判定超卖、允许做多的 RSI 阈值。 |
| `RsiOverbought` | `decimal` | `70` | 判定超买、允许做空的 RSI 阈值。 |
| `CorrelationLookback` | `int` | `60` | 参与皮尔逊相关计算的日线偏差数量。 |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | 盘中计算 EMA/RSI 的蜡烛类型。 |
| `DailyCandleType` | `DataType` | `TimeSpan.FromDays(1).TimeFrame()` | 构建相关性所需的高周期蜡烛类型。 |
| `HedgeSecurity` | `Security` | `null` | 对冲品种。启动前必须指定。 |

> **交易量说明：** 策略使用 StockSharp 的 `Volume` 属性作为两腿下单手数。程序会根据各品种的 `VolumeStep`、`VolumeMin` 与 `VolumeMax` 自动调整手数；当 `Volume` 未设置时默认使用 `0.1`。

## 开平仓规则
- **做多组合**
  - 条件：`fast EMA > slow EMA`、`RSI <= RsiOversold`、相关系数 < 0、无持仓。
  - 行为：基准品种与对冲品种均市价买入。
- **做空组合**
  - 条件：`fast EMA < slow EMA`、`RSI >= RsiOverbought`、相关系数 < 0、无持仓。
  - 行为：两腿同时市价卖出。
- **平仓**
  - 当浮动盈利达到 `TakeProfit` 时自动平掉两腿。
  - 一旦超出时间窗口，策略不再开新仓，从而保持与原始 EA 一致的保护逻辑。

## 实现要点
- 相关系数使用固定长度的缓冲区保存，模拟 MQL4 版本中 `Period()` 个历史值的滚动窗口。
- 日线偏差的计算与原版的 `iClose - iMA` 完全一致，均基于 `DailyCandleType` 提供的日线数据。
- 根据需求说明，本次未创建 Python 版本及其文件夹。
