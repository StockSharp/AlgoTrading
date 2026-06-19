# PosNegDiCrossoverStrategy

## 概述
**PosNegDiCrossoverStrategy** 是 MetaTrader 指标 `_HPCS_PosNegDIsCrossOver_Mt4_EA_V01_WE` 的 StockSharp 版本。原始 EA 根据 ADX
指标的 +DI 与 -DI 交叉来开仓，并为每笔交易设置对称的止盈/止损（以点数表示）。若发生亏损，会按照固定倍数放大手数再次进场，直至获利或达到指定的马丁格尔次数上限。

## 交易逻辑
1. **信号识别**：当新的完整 K 线到来时，策略获取最新的 ADX 值，并与上一根 K 线的 +DI/-DI 比较；若 +DI 从下向上穿越 -DI 触发做多，若 +DI 从上向下跌破 -DI 触发做空。为了复现 MQL 中的去重保护，每根 K 线仅允许一次初始入场。
2. **时间过滤**：只有在 `StartTime` 与 `StopTime` 所限定的交易时段内才允许开仓。时段之外，策略仍会跟踪已有仓位的虚拟止盈/止损，但不会启动新的交易循环或继续马丁格尔加仓。
3. **下单与转换**：触发信号后按照 `OrderVolume` 发送市价单。成交后，将 `TakeProfitPips`、`StopLossPips` 按照标的物的最小变动价位转换为绝对价格（若报价有 3 或 5 位小数，会乘以 10），并保存为后续平仓判定的价格。
4. **止盈止损处理**：每根完整 K 线都会检查价格区间。对于多头，当最低价触及止损或最高价触及止盈时，以市价单平仓；空头使用对称条件。这样可以在平仓后立即判断交易结果。
5. **马丁格尔循环**：若上一笔交易亏损，则将手数乘以 `MartingaleMultiplier` 并立即按原方向再次入场（仍需满足时间过滤）。一旦达到 `MartingaleCycleLimit` 次或出现盈利平仓，循环被重置，等待下一次 ADX 交叉。

## 参数
| 名称 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `CandleType` | 15 分钟时间框 | 用于计算 ADX 及监控止盈/止损的 K 线类型。 |
| `AdxPeriod` | 14 | ADX 指标的周期长度。 |
| `UseTimeFilter` | `true` | 是否启用交易时间过滤。 |
| `StartTime` | 00:00 | 允许开仓的开始时间（交易所时间）。 |
| `StopTime` | 23:59 | 允许开仓的结束时间（交易所时间）。 |
| `OrderVolume` | 0.1 | 初始市价单的交易手数。 |
| `TakeProfitPips` | 10 | 止盈距离（点数），转换成价格后用于虚拟止盈。 |
| `StopLossPips` | 10 | 止损距离（点数），转换成价格后用于虚拟止损。 |
| `MartingaleMultiplier` | 2 | 马丁格尔加仓时的手数倍增系数。 |
| `MartingaleCycleLimit` | 5 | 每个信号允许的最大马丁格尔次数。 |

## 说明
- 策略在下单前会调用 `IsFormedAndOnlineAndAllowTrading()`，确保所有订阅与风控状态已经准备就绪。
- 止盈/止损采用“虚拟”方式，模仿 MetaTrader 将保护单直接挂在持仓上的行为，同时保持对 StockSharp 高阶 API 的兼容。
- 如果将 `StartTime` 与 `StopTime` 设置为相同的时间，或关闭 `UseTimeFilter`，则策略会像原 EA 一样在全天候运行。
