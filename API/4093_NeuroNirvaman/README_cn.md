# Neuro Nirvaman MQ4 策略

## 概览
**Neuro Nirvaman MQ4 策略** 是 MetaTrader 4 专家顾问 `NeuroNirvaman.mq4` 的完整移植版本。原始 EA 将经过 Laguerre 滤波的 ADX 正向动向 (+DI) 与 SilverTrend 突破指示器结合，由三层感知器和一个监督器做出交易决策。StockSharp 版本复现了这一流程，只在收盘 K 线上重新计算，并且始终保持单一持仓。

## 工作流程
1. **市场数据** – 通过 `CandleType` 选择一条蜡烛序列，只处理状态为 `Finished` 的 K 线，等同于 MQL 中的 `Time[0]` 检查。
2. **Laguerre +DI 平滑** – 四个 `AverageDirectionalIndex` 指标提供 +DI 数值，交给 `LaguerrePlusDiState` 使用原始的 `γ = 0.764` 进行四阶 Laguerre 滤波，输出落在 `[0, 1]` 区间的振荡器值。每个流可以单独设置 ADX 周期和 0.5 附近的中性区宽度 (`Laguerre*Distance`)。
3. **SilverTrend 复制** – 两个 `SilverTrendState` 对象实现 `Sv2.mq4` 算法。它们对最近 `SSP` 根 K 线计算最高价和最低价，使用常量 `Kmax = 50.6` 缩放通道，当趋势向上时返回 `1`，趋势向下时返回 `-1`。`SilverTrend1Length` 与 `SilverTrend2Length` 控制各自的窗口长度。
4. **感知器链路** –
   - **感知器 #1** 将第一条 Laguerre 激活值与第一条 SilverTrend 信号按 `X11 - 100` 与 `X12 - 100` 的权重组合。
   - **感知器 #2** 对第二条 Laguerre 与第二条 SilverTrend 做同样的组合，使用 `X21 - 100` 与 `X22 - 100`。
   - **感知器 #3** 仅使用第三和第四条 Laguerre 激活值，权重为 `X31 - 100` 与 `X32 - 100`。
   每条 Laguerre 激活值都会根据与 0.5 的距离被量化为 `-1`、`0` 或 `1`。
5. **监督器 (`Pass`)** – 完全复现 MQL 中的 `Supervisor()` 逻辑：
   - `Pass = 3` 时，若感知器 #3 > 0 且感知器 #2 > 0，则按第二组止盈/止损买入；否则若感知器 #1 < 0，则按第一组止盈/止损卖出。
   - `Pass = 2` 时，感知器 #2 > 0 触发多单（第二组止盈/止损），否则触发空单（第一组止盈/止损）。
   - `Pass = 1` 时，感知器 #1 < 0 触发空单，否则触发多单；两条分支都使用第一组止盈/止损距离。
6. **下单与风控** – 使用 `BuyMarket` 或 `SellMarket` 以 `TradeVolume` 设定的手数建仓。止盈/止损价格按照 `entry ± points * PriceStep` 计算。由于 StockSharp 只发送市价单，策略会在每根完成的 K 线中检查最高价和最低价，从而模拟 MT4 服务器端的 TP/SL 行为。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 15 分钟 | 策略使用的蜡烛类型。 |
| `TradeVolume` | `decimal` | 0.1 | 下单手数。 |
| `SilverTrend1Length` | `int` | 7 | 第一条 SilverTrend 的窗口长度 (SSP)。 |
| `Laguerre1Period` | `int` | 14 | 第一条 Laguerre 流的 ADX 周期。 |
| `Laguerre1Distance` | `decimal` | 0 | 第一条 Laguerre 流在 0.5 周围的中性区宽度（百分比）。 |
| `X11`, `X12` | `decimal` | 100 | 感知器 #1 的权重（在计算前会减去 100）。 |
| `TakeProfit1`, `StopLoss1` | `decimal` | 100 / 50 | 第一组风险参数（点数），同时用于所有空头仓位。 |
| `SilverTrend2Length` | `int` | 7 | 第二条 SilverTrend 的窗口长度。 |
| `Laguerre2Period` | `int` | 14 | 第二条 Laguerre 流的 ADX 周期。 |
| `Laguerre2Distance` | `decimal` | 0 | 第二条 Laguerre 流的中性区宽度。 |
| `X21`, `X22` | `decimal` | 100 | 感知器 #2 的权重。 |
| `TakeProfit2`, `StopLoss2` | `decimal` | 100 / 50 | 第二组风险参数（点数）。 |
| `Laguerre3Period`, `Laguerre4Period` | `int` | 14 | 第三、第四条 Laguerre 流的 ADX 周期。 |
| `Laguerre3Distance`, `Laguerre4Distance` | `decimal` | 0 | 第三、第四条 Laguerre 流的中性区宽度。 |
| `X31`, `X32` | `decimal` | 100 | 感知器 #3 的权重。 |
| `Pass` | `int` | 3 | 选择监督器的运行分支。 |

## 使用建议
- 权重保持在 `100` 时，相应输入会被抵消。若需要激活感知器，需将权重偏离 100。
- SilverTrend 需要累积足够的历史 K 线后才会输出 `±1`，在此之前感知器结果会为 0，与 MT4 `iCustom` 的缓冲行为一致。
- 止盈/止损通过完成 K 线的最高价和最低价模拟执行，若实际市场在两根 K 线之间出现极端波动，结果可能与真实经纪商存在差异。
- 策略始终保持单仓运行，在当前仓位关闭前会忽略新的信号。
- 请将 `CandleType` 设置为与 MT4 中使用的图表周期一致（如 M15、H1），以确保指标尺度相符。
