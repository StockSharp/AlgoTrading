# MA MACD Position Averaging v2 策略

## 概述
**MA MACD Position Averaging v2** 策略源自 Vladimir Karputov 的 MetaTrader 专家顾问。它将加权移动平均线过滤器、MACD 确认模块以及在行情不利时扩大量的网格化加仓模块组合在一起。StockSharp 版本完整保留原始信号顺序，在每根完成的 K 线上计算指标，并在代码中实现止损、止盈和追踪止损，以模拟 MQL 端的券商逻辑。

## 交易逻辑
1. **指标准备**
   - 可配置的移动平均线应用于指定的蜡烛类型与价格字段。`MaShift` 参数通过读取更早的蜡烛值来模拟 MetaTrader 的正向平移，`BarOffset` 允许选择当前或更早的柱体进行评估。
   - MACD 指标按照可调的快/慢/信号周期和应用价格生成主线与信号线，与原策略保持一致。
2. **信号校验**
   - 多头条件：两条 MACD 线均为负值，价格高于平移后的移动平均线，且价格与移动平均线之间的距离不少于 `MaIndentPips`（基于品种点值转换为绝对价格）。
   - 空头条件：两条 MACD 线均为正值，价格低于平移后的移动平均线，且与平均线的距离不少于 `MaIndentPips`。
   - 比率过滤：`MacdRatio` 要求满足 `MACD_main / MACD_signal >= MacdRatio`，全部计算在十进制精度下完成。
   - 当 `ReverseSignals = true` 时，在所有过滤通过后将信号方向取反。
3. **持仓生命周期**
   - **无仓位** 时，按 `OrderVolume`（经交易品种的最小步长四舍五入）发出市价单，同时根据 `StopLossPips` 与 `TakeProfitPips` 设定初始止损与止盈价格。
   - **已有持仓** 时，策略不会开立反向仓位：
     - 若同时发现多头与空头（安全检查），立即全部平仓；
     - 否则触发当前方向的加仓模块。
4. **加仓模块**
   - 多头：寻找开仓价最低且浮亏超过 `StepLossPips` 的持仓腿；空头：寻找开仓价最高且浮亏超过阈值的腿。
   - 找到候选腿后，按照 `候选腿手数 × LotCoefficient` 计算新订单量，并根据最小/最大手数及步长进行调整，复刻 MQL 中的几何倍数加仓逻辑。
   - 新腿继承原有的止损、止盈距离，并参与追踪止损更新。
5. **风控处理**
   - 仅当 `TrailingStopPips` 与 `TrailingStepPips` 均大于零时启用追踪止损。多头在浮盈超过 `TrailingStopPips + TrailingStepPips` 后，将止损抬至 `收盘价 - TrailingStopPips`；空头逻辑相反。
   - 每根完成的 K 线都会检查是否触发止损或止盈，一旦命中，立即以市价关闭该腿并从加仓列表移除。

## 参数
| 参数 | 说明 |
| --- | --- |
| **OrderVolume** | 第一笔交易的基础手数。 |
| **StopLossPips** | 止损距离（点），为 0 表示不设置止损。 |
| **TakeProfitPips** | 止盈距离（点），为 0 表示不设置止盈。 |
| **TrailingStopPips** | 追踪止损与当前价格的目标距离，与 `TrailingStepPips` 配合使用。 |
| **TrailingStepPips** | 启动下一次追踪前所需的额外盈利点数。 |
| **StepLossPips** | 触发加仓所需的最小浮亏（点）。 |
| **LotCoefficient** | 对选定浮亏腿应用的手数倍数。 |
| **BarOffset** | 指标取值所回看的柱数（0 表示当前完成柱）。 |
| **ReverseSignals** | 通过此开关反转交易方向而不改变过滤条件。 |
| **MaPeriod** | 移动平均线周期。 |
| **MaShift** | 移动平均线前移量（MetaTrader 风格）。 |
| **MaMethod** | 移动平均线类型（Simple、Exponential、Smoothed、Weighted）。 |
| **MaPrice** | 计算移动平均线所使用的蜡烛价格。 |
| **MaIndentPips** | 价格与移动平均线之间的最小距离。 |
| **MacdFastPeriod** | MACD 快速 EMA 周期。 |
| **MacdSlowPeriod** | MACD 慢速 EMA 周期。 |
| **MacdSignalPeriod** | MACD 信号线 EMA 周期。 |
| **MacdPrice** | MACD 使用的价格类型。 |
| **MacdRatio** | MACD 主线与信号线的最小比值。 |
| **CandleType** | 策略订阅的蜡烛类型。 |

## 实现细节
- 点值通过 `Security.PriceStep` 计算，并针对 3/5 位报价进行修正，与原版 EA 的点值定义保持一致。
- 使用队列缓存移动平均与 MACD 的历史值，模拟 `ma_shift` 和 `BarOffset` 行为，同时遵守仓库禁止直接获取历史数据的规则。
- 手数计算自动考虑 `Security.VolumeStep`、`Security.MinVolume` 与 `Security.MaxVolume`，避免倍数放大后出现非法数量。
- 止损/止盈/追踪止损完全在策略层实现，不依赖券商侧的订单修改接口。
- 代码位于 `StockSharp.Samples.Strategies` 命名空间，遵循仓库要求使用制表符缩进，并仅包含英文注释。
