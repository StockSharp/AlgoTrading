# FuturePatternMemoryStrategy

## 概述
`FuturePatternMemoryStrategy` 是 MetaTrader 经典专家顾问 **FutureMA** 与 **FutureMACD** 的 StockSharp 版本。原始程序会将指标差值序列写入 CSV 文件，通过历史统计判断当前环境更适合多头还是空头突破。本策略保留了同样的思想，但把文件持久化改为内存中的模式库，并把所有关键选项暴露为参数。通过 `Source` 参数可以在平滑移动平均价差（FutureMA 逻辑）和 MACD 柱状图（FutureMACD 逻辑）之间切换。

每根完成的蜡烛会按照以下五个步骤处理：

1. **指标投影**：计算所选振荡器（SMMA 价差或 MACD 柱状图），按可调的 `NormalizationFactor` 进行缩放，并离散化为整数以构建紧凑的模式签名。
2. **模式哈希**：维护最近 `AnalysisBars` 个离散值的滑动窗口。每次有新蜡烛收盘时，该窗口被转换成一个唯一的哈希字符串，用于标识当前的市场环境。
3. **历史摆动分析**：回溯 `FractalDepth` 根蜡烛，测量最早那根蜡烛开盘价到最高点/最低点的距离，并换算成点值。这些距离就是原始 EA 在 CSV 中累计的盈利期望。
4. **加权记忆更新**：使用哈希键在字典中获取或创建模式条目，通过 `(current + input * ForgettingFactor) / (1 + ForgettingFactor)` 的公式更新多空期望，从而复刻 MQL 代码中的 “遗忘系数” (`zabyvaemost`)。
5. **信号评估与执行**：如果多头期望优于空头期望，出现次数超过 `MinimumMatches`，且预期盈利大于 `MinimumTakeProfit`，策略便开仓或加仓多头；空头逻辑完全对称。止损、止盈来源于累积的统计数据，并可在价格前进时按原始算法进行四分位追踪。

## 移植说明
- 通过单一策略整合了两个 EA，可用 `Source` 参数在 MA 模式和 MACD 模式之间切换，无需重新编译。
- 文件系统被 `Dictionary<string, PatternStats>` 所取代，所有统计数据都保存在内存中，符合 StockSharp 沙箱环境要求。
- 仓位管理沿用原版：止损使用完整的平均摆动距离，止盈使用 `StatisticalTakeRatio`（即 `Stat_Take_Profit`），`EnableTrailingStop` 为真时按照原脚本以利润距离的四分之一移动止损。
- `ManualMode` 对应原始的 `Ruchnik`，允许只收集统计数据而不发出订单。
- `AllowAddOn` 对应 `dokupka`，在新蜡烛重复出现同一模式时允许加仓。

## 交易逻辑详情
- **指标来源**
  - *MA Spread*：在中位价上计算 6 周期与 24 周期的 SMMA，使用两者的差值。
  - *MACD Histogram*：计算 MACD 主线与信号线的差值（默认 12/26/9）。
- **离散化**：`NormalizationFactor` 重现了 `tocnost` 的作用，将原始差值缩放后除以 `100 * MinPriceStep` 并取整。
- **模式记忆**：字典为每个哈希保存多头次数、多头平均距离、空头次数与空头平均距离，并按加权平均更新。
- **入场条件**：
  - 多头：多头期望 ≥ 空头期望，出现次数 > `MinimumMatches`，且期望距离 > `MinimumTakeProfit`。
  - 空头：空头期望 ≥ 多头期望，出现次数 > `MinimumMatches`，且期望距离 > `MinimumTakeProfit`。
- **风控**：止损 = 平均摆动距离；止盈 = `StatisticalTakeRatio` × 平均摆动距离；若启用追踪，则在价格走出四分之一距离后相应上移/下移止损。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 使用的主时间框架。 | 30 分钟 |
| `Source` | 选择 MA 价差或 MACD 柱状图。 | `MaSpread` |
| `FastMaLength` / `SlowMaLength` | 当 `Source = MaSpread` 时的 SMMA 长度。 | 6 / 24 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | 当 `Source = MacdHistogram` 时的 MACD 参数。 | 12 / 26 / 9 |
| `AnalysisBars` | 构成模式哈希的蜡烛数量。 | 8 |
| `FractalDepth` | 用于测量摆动区间的历史蜡烛数量。 | 4 |
| `MinimumMatches` | 触发交易所需的最少出现次数。 | 5 |
| `MinimumTakeProfit` | 接受信号所需的最小预期点数。 | 30 |
| `NormalizationFactor` | 指标差值的缩放系数。 | 10 |
| `ForgettingFactor` | 模式记忆中新增数据的权重。 | 1.5 |
| `StatisticalTakeRatio` | 止盈相对摆动距离的比例。 | 0.5 |
| `EnableTrailingStop` | 是否启用四分位追踪止损。 | `false` |
| `ManualMode` | 仅统计不下单。 | `false` |
| `AllowAddOn` | 允许重复信号加仓。 | `true` |
| `Volume` | 下单手数。 | 0.1 |

## 实战建议
- 模式哈希依赖离散化，`NormalizationFactor` 与 `AnalysisBars` 过大将导致哈希过于稀疏，过小则会把不同状态混为一谈，请结合品种特性调节。
- 若需要跨会话保留记忆，可在回测或实盘结束后将字典序列化并自行保存。
- 原 EA 按品种与周期独立存储数据，建议在 StockSharp 中也为每个品种/周期单独运行一个实例，避免统计混淆。
