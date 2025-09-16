# Neuro Nirvaman 策略

## 概述
Neuro Nirvaman 策略是 MetaTrader 5 专家顾问 *NeuroNirvamanEA* 的完整移植版本。策略保持了原始 MQL 代码中的感知机决策树：通过四个 Laguerre 平滑的正向动向指标（+DI）与两个 SilverTrend 波段信号的组合来产生买卖指令。策略仅在收盘完成的 K 线数据上运行，根据参数计算市场价买入或卖出，并在每笔交易时设置点数形式的止盈/止损，不会加仓或同时持有多头与空头。

## 数据输入与指标
- **四个 AverageDirectionalIndex**：每个实例都使用独立的周期设置。策略只读取其中的 +DI 分量，并通过 Laguerre 滤波器处理，得到介于 `[0, 1]` 之间的平滑值。
- **LaguerrePlusDiState**：内部辅助类，重现 `laguerre_plusdi.mq5` 自定义指标的全部算法，包括四级 Laguerre 平滑和 `CU / (CU + CD)` 归一化过程。
- **两个 SilverTrendState**：严格按照 `silvertrend_signal.mq5` 实现的波段信号。以 `SSP = 9` 的窗口分析最近 10 根 K 线，输出 `1`（出现向下箭头）、`-1`（出现向上箭头）或 `0`（无信号）。
- **K 线订阅**：通过 `CandleType` 参数选择时间框架，仅对状态为 `Finished` 的 K 线进行处理。

## 交易逻辑
1. **信号预处理**
   - 对每个 Laguerre 输出调用 `ComputeTensionSignal`：当值高于 `0.5 + distance/100` 返回 `-1`，低于 `0.5 - distance/100` 返回 `1`，否则为 `0`。
   - SilverTrend 在每根 K 线更新，其风险参数 (`Risk1`, `Risk2`) 用于放宽或收紧支撑/阻力区间，与 MT5 指标一致。
2. **感知机层**
   - **感知机 1** 将第一组 Laguerre 激活值与第一个 SilverTrend 信号组合，权重分别为 `X11 - 100` 与 `X12 - 100`。
   - **感知机 2** 将第二组 Laguerre 激活值与第二个 SilverTrend 信号组合，权重分别为 `X21 - 100` 与 `X22 - 100`。
   - **感知机 3** 只使用第三、第四个 Laguerre 激活值，权重为 `X31 - 100` 与 `X32 - 100`。
3. **Supervisor（Pass 参数）**
   - `Pass = 3`：若感知机 3 > 0 且感知机 2 > 0，则以 `TakeProfit2`/`StopLoss2` 开多；否则若感知机 1 < 0，则以 `TakeProfit1`/`StopLoss1` 开空。
   - `Pass = 2`：当感知机 2 > 0 时使用第二组风险参数买入，否则使用第一组风险参数卖出。
   - `Pass = 1`：当感知机 1 < 0 时卖出，否则买入，均使用第一组风险参数。
4. **仓位管理**
   - 下单使用 `BuyMarket`、`SellMarket`，成交量由 `TradeVolume` 指定。
   - 止盈/止损价基于信号 K 线的收盘价，计算公式为 `entry ± points * PriceStep`。随后在每根已完成 K 线中利用最高价/最低价检查触发情况，模拟 MT5 中的保护单行为。
   - 持仓未平仓时忽略新的入场信号，只有在仓位退出后才会评估下一次交易。

## 参数说明
| 名称 | 类型 | 默认值 | 描述 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 15 分钟 | 用于计算的 K 线类型。 |
| `TradeVolume` | `decimal` | 0.1 | 下单手数。 |
| `Risk1`, `Risk2` | `int` | 3 / 9 | SilverTrend 风险系数，控制区间宽度。 |
| `Laguerre1Period` – `Laguerre4Period` | `int` | 14 | 每个 Laguerre 滤波器的 ADX 周期。 |
| `Laguerre1Distance` – `Laguerre4Distance` | `decimal` | 0 | 以百分比表示的中性带宽度。 |
| `X11`, `X12`, `X21`, `X22`, `X31`, `X32` | `decimal` | 100 | 权重参数，使用前会减去 100。 |
| `TakeProfit1`, `StopLoss1`, `TakeProfit2`, `StopLoss2` | `int` | 100 / 50 | 以点数表示的止盈止损距离。 |
| `Pass` | `int` | 3 | 选择参与决策的感知机组合。 |

## 使用建议
- 默认权重为 100，会使感知机输出为 0。要激活策略，需要根据品种调整权重，使输出出现非零值。
- SilverTrend 的实现保留了原始状态变量（不含弹窗报警），因此信号时间与 MT5 版本保持一致。
- 止盈止损通过已完成 K 线的最高价/最低价判断，不模拟 K 线内部的瞬时波动。
- 策略仅支持单品种运行，同一时间只持有一个方向的仓位。
- 可使用参数元数据进行优化（所有参数均可通过 `Param()` 调整和优化）。

## 部署步骤
1. 编译解决方案，在 StockSharp 示例启动器或自建项目中加载该策略。
2. 选择标的、配置 K 线数据源，并根据交易思路调整权重与风险参数。
3. 启动策略，系统会自动创建带有 Laguerre 指标与成交记录的图表用于监控。
4. 需要批量测试或寻优时，可使用 StockSharp 内建的优化功能。
