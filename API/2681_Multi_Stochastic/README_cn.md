# Multi Stochastic 策略

## 概览
Multi Stochastic 策略是 MetaTrader 5 指标「Multi Stochastic (barabashkakvn's edition)」的 StockSharp 高级 API 改写版本。策略最多可以同时跟踪四个外汇品种，为每个品种计算参数为 5、3、3 的随机指标。当随机指标在超卖或超买区域内发生交叉时，策略会开仓，且每个品种始终只持有一笔仓位，并通过固定的点数止损 / 止盈离场。

## 交易逻辑
- 每个启用的品种都会创建一个独立的 StochasticOscillator 指标，长度为 5，%K 和 %D 平滑周期均为 3。
- 多头信号条件：当前 %K 低于 `OversoldLevel`（默认 20），上一根 K 线的 %K 低于 %D，且当前 K 线的 %K 向上穿越 %D。
- 空头信号条件：当前 %K 高于 `OverboughtLevel`（默认 80），上一根 K 线的 %K 高于 %D，且当前 K 线的 %K 向下跌破 %D。
- 当某个品种已经持有仓位时，新的信号会被忽略，直到仓位被平掉。

## 风险管理
- 止损和止盈以点数设置。策略会使用 `Security.PriceStep` 并对三位和五位报价做 10 倍调整，将点数转换为绝对价格距离（点 = 步长 × 10）。
- 多头仓位在 K 线最低价触及止损价或最高价到达止盈价时平仓。
- 空头仓位在 K 线最高价触及止损价或最低价到达止盈价时平仓。
- 如果交易品种缺少必要的元数据导致无法计算点值，相应的止损和止盈会被禁用，以避免错误的风控处理。

## 参数说明
- `CandleType`：所有订阅数据的 K 线周期（默认 1 小时）。
- `StochasticLength`：随机指标的基础长度（默认 5）。
- `StochasticKPeriod`：%K 平滑周期（默认 3）。
- `StochasticDPeriod`：%D 平滑周期（默认 3）。
- `OversoldLevel`：判定超卖的阈值（默认 20）。
- `OverboughtLevel`：判定超买的阈值（默认 80）。
- `StopLossPips`：止损距离（点数，默认 50）。
- `TakeProfitPips`：止盈距离（点数，默认 10）。
- `UseSymbol1` … `UseSymbol4`：是否启用对应的品种插槽（默认 true）。
- `Symbol1` … `Symbol4`：各插槽对应的交易品种，若 `Symbol1` 未指定则回退至策略主品种。

## 实现细节
- 每个品种都通过独立的 `SubscribeCandles` 订阅获取 K 线，并结合 `BindEx` 得到 `StochasticOscillatorValue`，避免手动处理历史缓冲区。
- 策略为每个品种缓存上一根 K 线的 %K 和 %D 值，精确复刻 MT5 版本的交叉判断。
- 每次开仓都会重新计算止损与止盈价位；仓位平掉或不存在时会清除这些水平。
- 交易指令使用 `BuyMarket` / `SellMarket`，共享基类的 `Volume` 参数，从而保持单仓位约束。

## 与 MT5 版本的差异
- StockSharp 版本完全依赖高级订阅机制，不再需要手动刷新报价。
- 点值通过 `Security.PriceStep` 与 `Security.Decimals` 推导，如果行情信息不完整，策略会跳过止损止盈设置。
- 代码中保留了英文注释和可扩展的日志/图表接口，便于进一步开发。

## 使用建议
1. 为 `Symbol1`…`Symbol4` 设置所需的交易品种，并调整 `CandleType` 以匹配交易周期。
2. 检查止损 / 止盈点数是否与品种的最小跳动单位匹配，避免因过小的设置导致即时平仓。
3. 对于不需要的品种插槽，可将 `UseSymbolX` 设为 false 以减少数据订阅开销。
