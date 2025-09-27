# RAVI + Awesome Oscillator 策略

## 概述
- 将 MetaTrader 5 专家顾问“Ravi AO (barabashkakvn's edition)”移植到 StockSharp 高级 API。
- 结合 RAVI（区间动作验证指标）与 Awesome Oscillator，用于捕捉同步的多空动量切换。
- 适用于 StockSharp 支持的任意周期与品种，所有距离均以点（pip）为单位，与原策略保持一致。

## 指标
- **RAVI**：按 `100 * (FastMA - SlowMA) / SlowMA` 公式计算，使用所选价格序列。可在简单、指数、平滑、加权移动平均之间选择，并可设置周期以及价格类型（收盘、开盘、最高、最低、中值、典型价、加权价、简化价、四分位价、TrendFollow、Demark）。
- **Awesome Oscillator**：基于中价的动量指标，可设置快慢周期，默认值 5 与 34 与 MT5 保持一致。

## 参数
| 参数 | 说明 |
| --- | --- |
| `CandleType` | 订阅的蜡烛/数据类型。 |
| `StopLossPips` | 止损距离（点）。`0` 表示不使用止损。 |
| `TakeProfitPips` | 止盈距离（点）。`0` 表示不使用止盈。 |
| `TrailingStopPips` | 基础跟踪止损距离（点）。`0` 表示关闭跟踪止损。 |
| `TrailingStepPips` | 每次调整跟踪止损所需的额外盈利（点）。启用跟踪止损时必须大于 0。 |
| `FastMethod` / `FastLength` | RAVI 快速均线的类型与周期。 |
| `SlowMethod` / `SlowLength` | RAVI 慢速均线的类型与周期。 |
| `AppliedPrices` | 两条均线所使用的价格公式（close、open、high、low、median、typical、weighted、simple、quarter、trend-follow #1/#2、Demark）。 |
| `AoShortPeriod` / `AoLongPeriod` | Awesome Oscillator 的快、慢周期。 |

## 交易规则
1. 仅在蜡烛收盘 (`CandleStates.Finished`) 后更新指标。
2. **做多条件**：
   - AO 在前两根柱子中由负转正（两根前 `< 0`，一根前 `> 0`），并且
   - RAVI 在前两根柱子中由负转正。
3. **做空条件**：
   - AO 在前两根柱子中由正转负，且
   - RAVI 在前两根柱子中由正转负。
4. 同一时间只允许持有一笔仓位，有仓位时忽略新的入场信号。

## 离场管理
- **止损**：按照 `StopLossPips` 与品种的 `Security.PriceStep` 计算。对于 5 位或 3 位报价的外汇对，会将步长放大 10 倍，模拟 MT5 中的点值。蜡烛的最高/最低触及止损价即视为触发。
- **止盈**：同样方式计算，可通过将参数设为 `0` 关闭。
- **跟踪止损**：当未实现盈亏超过 `TrailingStopPips + TrailingStepPips` 时，更新止损。做多将止损移动到 `ClosePrice - TrailingStopPips`，做空则为 `ClosePrice + TrailingStopPips`。
- 所有离场操作均使用市价单一次性平仓。

## 实现说明
- 信号在收盘时评估，并在同一根蜡烛的收盘价执行。原版 MT5 在下一根开盘进场，因此可能存在差异。
- 仅使用 StockSharp 提供的均线类型，MT5 中的 JJMA、Jurik、T3 等特殊平滑方式暂不支持。
- 原指标的绘图位移 `Shift` 只影响显示，对交易无影响，故未加入参数。
- `AppliedPrices` 的计算公式完全按照 MetaTrader 的定义实现。

## 使用建议
- 策略偏向趋势跟随，可配合更高周期的趋势或波动性过滤器使用。
- 不同品种的步长不同，建议针对每个市场优化周期与点差设置。
- 如需在交易所侧托管止损，可结合 `Strategy.StartProtection` 或外部风控模块。
