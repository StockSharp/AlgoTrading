# Williams AO + AC 策略

## 概述
**Williams AO + AC 策略** 将 MetaTrader 4 专家顾问 “Williams_AOAC” 转换为 StockSharp 的高层策略 API。该策略把多种 Bill Williams 工具组合在一起，以在默认的 1 小时时间框中捕捉动量爆发：

1. **布林带宽过滤** —— 只有当布林带上下轨之间的间距落在可配置的点值区间内时才允许交易，从而同时避免盘整和过度波动的行情。
2. **RSI 动量确认** —— 多头必须让 RSI 高于多头阈值，空头则必须让 RSI 低于空头阈值。
3. **Awesome Oscillator 零轴穿越** —— 指标需要在当前柱上穿越零轴，表明动量方向出现变化。
4. **Accelerator Oscillator 加速** —— 最近三根柱的 AO 加速值必须位于同一侧，并且最新值要进一步扩大动量，以确认加速过程。
5. **交易时段过滤** —— 只有在指定的日内时间窗口内才允许发出信号。

每根收盘的蜡烛都会触发 `Bind` 管道提供的指标数据。如果所有过滤条件都成立，策略会平掉反向仓位并按照设定手数开立新的市价单，同时根据点数距离设置止损和止盈。可选的追踪止损会在浮动盈利达到阈值后上移或下移保护价位。

## 入场逻辑
### 多头条件
1. 将布林带宽换算为点值后，必须落在 **BollingerSpreadLower** 与 **BollingerSpreadUpper** 之间。
2. RSI 数值必须严格大于 **RsiBuyThreshold**。
3. Awesome Oscillator 需要在当前柱上从负值穿越到正值。
4. 最近三根柱的加速振荡器数值均为正，而且最新值大于上一根值，说明上涨动量正在增强。
5. 当前蜡烛的开盘时间位于 **EntryHour** 开始、持续 **TradingWindowHours** 小时的交易窗口中（若超过午夜则会自动环绕）。
6. 当前没有持有多头仓位（可以是空仓或空头）。

当满足全部条件时，策略会先平掉空头，然后以 **TradeVolume** 手数开多，并重新设置止损 / 止盈。只有当价格至少朝有利方向运行 **TrailingStopPoints** 点之后，追踪止损才会开始生效。

### 空头条件
1. 布林带宽位于允许范围之内。
2. RSI 数值必须严格小于 **RsiSellThreshold**。
3. Awesome Oscillator 需要在当前柱上从正值跌破到负值。
4. 最近三根柱的加速振荡器数值均为负，而且最新值小于上一根值，表明下行压力正在增强。
5. 蜡烛的开盘时间位于有效交易时段内。
6. 当前没有持有空头仓位（可以是空仓或多头）。

满足条件后模块会平掉多头仓位，按 **TradeVolume** 手数开立空头，并重新附加保护单。

## 离场与风控
* **止盈** —— 当 **TakeProfitPoints** 大于零时，会在入场价的指定点数位置挂出止盈单。
* **止损** —— 当 **StopLossPoints** 大于零时，会按距离入场价的点数设置固定止损。
* **追踪止损** —— 当 **TrailingStopPoints** 大于零时，一旦浮动盈利超过该距离，就会把止损向盈利方向移动。多头会把止损抬高到 `Close - TrailingStopPoints * pip`，空头则把止损下移到 `Close + TrailingStopPoints * pip`，并且不会回撤。
* 用户手动调仓会被及时识别，追踪逻辑依据策略当前的聚合持仓进行调整。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 进行计算的主蜡烛序列。 | 1 小时蜡烛 |
| `BollingerPeriod` | 布林带周期。 | 20 |
| `BollingerDeviation` | 标准差倍数。 | 2.0 |
| `BollingerSpreadLower` | 启用交易所需的最小带宽（点）。 | 40 |
| `BollingerSpreadUpper` | 允许交易的最大带宽（点）。 | 210 |
| `AoFastPeriod` | Awesome Oscillator 的快周期。 | 11 |
| `AoSlowPeriod` | Awesome Oscillator 的慢周期。 | 40 |
| `RsiPeriod` | RSI 的计算周期。 | 20 |
| `RsiBuyThreshold` | 多头所需的最小 RSI 值。 | 46 |
| `RsiSellThreshold` | 空头所需的最大 RSI 值。 | 40 |
| `EntryHour` | 交易窗口的起始小时（0–23）。 | 0 |
| `TradingWindowHours` | 交易窗口持续的小时数（0 表示仅允许起始小时）。 | 20 |
| `TradeVolume` | 每次入场的手数。 | 0.01 |
| `StopLossPoints` | 止损距离（点）。 | 60 |
| `TakeProfitPoints` | 止盈距离（点）。 | 90 |
| `TrailingStopPoints` | 追踪止损距离（点）。 | 30 |

## 其他说明
* 策略内部通过用当前 Awesome Oscillator 值减去其 5 周期简单移动平均来获得 Accelerator Oscillator，与原版 MetaTrader 指标的逻辑保持一致。
* 带宽换算依赖交易品种的 `PriceStep`。若该信息不可用，则退回到直接使用价格差。
* 当 `EntryHour + TradingWindowHours` 超过 23 时，交易时间窗口会跨日循环，完全复现原始小时过滤器的行为。
* 策略在开仓前会自动平掉反向持仓，从而保持与原 MQL4 程序“一次仅一个方向仓位”的限制一致。
