# ASCV BrainTrend Signal 策略

**ASCV BrainTrend Signal** 策略是原始 MetaTrader Expert Advisor 的移植版本，基于 BrainTrend1 指标的信号执行交易。StockSharp 实现通过高层 API 结合 ATR、随机振荡指标以及 Jurik 移动平均 (JMA)，用于识别动量反转并设置可选的止损、止盈和跟踪止损。

## 策略思路

1. 使用 ATR 计算当前波动率，得到用于确认信号的动态阈值。
2. 对收盘价应用 Jurik 平滑，并与两根之前的 JMA 值比较。
3. 当平滑差值大于 `ATR / 2.3` 时，根据随机指标 `%K` 判断方向：
   - `%K < 47`：进入潜在做空状态。
   - `%K > 53`：进入潜在做多状态。
4. 信号在下一根完成的 K 线上执行，可通过 **ReverseSignals** 参数反转多空逻辑。
5. 止损、止盈及跟踪止损以“点”(最小报价步长的倍数)表示。

## 开平仓规则

- **做多**：上一根 K 线生成买入信号，且当前仓位不是多头。下单量为 `Volume + |Position|`，若有空头仓位会先行平仓。
- **做空**：上一根 K 线生成卖出信号，且当前仓位不是空头。
- **止损**：`entry ± StopLossPips * priceStep`，若下一根 K 线触及该价位则市价平仓。
- **止盈**：`entry ± TakeProfitPips * priceStep`（参数大于 0 时启用）。
- **跟踪止损**：当 `TrailingStopPips` 和 `TrailingStepPips` 都大于 0 时启用。价格向有利方向移动 `TrailingStopPips + TrailingStepPips` 后，止损价向有利方向移动 `TrailingStopPips`。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `AtrPeriod` | ATR 波动率周期。 | 14 |
| `StochasticPeriod` | 随机指标基础周期。 | 12 |
| `JmaLength` | Jurik 平滑长度。 | 7 |
| `StopLossPips` | 止损点数。 | 15 |
| `TakeProfitPips` | 止盈点数。 | 46 |
| `TrailingStopPips` | 跟踪止损距离。 | 0 (关闭) |
| `TrailingStepPips` | 跟踪止损触发步长。 | 5 |
| `ReverseSignals` | 反转多空信号。 | false |
| `CandleType` | 工作时间框架 (默认 15 分钟)。 | 15m |

## 使用提示

- 仅在完成的 K 线上计算指标，可减少盘中噪声。
- 若 `Security.MinPriceStep` 不可用，会使用默认步长 `0.0001` 将点值转换为价格差。
- 图表会绘制蜡烛图、随机指标和 JMA，方便实时监控策略状态。
- 跟踪止损实现与原始 EA 一致，只会向盈利方向移动，并要求达到距离和步长的双重条件。

## 建议

- 根据交易品种的波动率调整 `AtrPeriod` 与 `StochasticPeriod`。
- 对于最小报价步长较大的资产，适当增大止损和止盈，避免过早出场。
- 需要反向运行时可启用 `ReverseSignals` 参数。
- 实际交易应结合券商风控或其他风险管理手段。
