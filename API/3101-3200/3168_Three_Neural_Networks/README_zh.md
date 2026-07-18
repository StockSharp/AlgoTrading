# 三个神经网络策略

## 概述

该策略是 MetaTrader 智能交易系统“三个神经网络”的 StockSharp 高级 API 版本。策略通过订阅三种不同周期（H1、H4、D1）的蜡烛，并使用内置的 `SmoothedMovingAverage` 指标来重建原版中的三个神经层，全部逻辑都在 StockSharp 的高级框架内完成。

## 工作流程

1. 启动时分别订阅 H1、H4、D1 的蜡烛数据，并绑定基于中价的平滑移动平均值，以复现 MetaTrader 中 `iMA(..., MODE_SMMA, PRICE_MEDIAN)` 的调用方式。
2. 每个周期都会维护一个考虑到偏移量的滚动窗口。当收集到四个偏移后的值时，就会按照原策略的加权差分公式计算三个神经元输出，并把结果四舍五入到四位小数。
3. 每当 H1 蜡烛收盘时，策略会组合三个神经元输出：
   - 三个输出都为正 → 建立或维持多头仓位；
   - H1 输出为正且 H4、D1 输出为负 → 建立或维持空头仓位。
4. 持仓量可在固定手数与风险百分比两种模式之间切换。风险模式下会按照投资组合当前价值的 `VolumeOrRisk` 百分比估算资金，并用当前价格转换为交易量。
5. 风险控制逻辑继承自 EA：在仓位方向发生变化后立即根据点值设置止损与止盈，并在每根 H1 蜡烛收盘时，如果价格突破“拖尾距离 + 拖尾步长”，就重新收紧拖尾止损。
6. 每根完成的 H1 蜡烛都会先检查当前止损或止盈是否被触发，如满足条件就通过 `ClosePosition()` 以市价平仓；`EnableDetailedLog` 参数可输出与原 EA `InpPrintLog` 类似的详细日志。

## 参数

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `StopLossPips` | `50` | 止损距离（点）。为 `0` 时禁用止损。 |
| `TakeProfitPips` | `50` | 止盈距离（点）。为 `0` 时禁用止盈。 |
| `TrailingStopPips` | `15` | 拖尾止损距离。 |
| `TrailingStepPips` | `5` | 每次调整拖尾止损所需的最小改善幅度。 |
| `ManagementMode` | `RiskPercent` | 资金管理模式：`FixedLot` 表示固定手数，`RiskPercent` 表示风险百分比。 |
| `VolumeOrRisk` | `1` | 固定手数或风险百分比（取决于资金管理模式）。 |
| `H1Period`, `H1Shift` | `2`, `5` | H1 平滑均线的周期与偏移。 |
| `H4Period`, `H4Shift` | `2`, `5` | H4 平滑均线的周期与偏移。 |
| `D1Period`, `D1Shift` | `2`, `5` | D1 平滑均线的周期与偏移。 |
| `P1`, `P2`, `P3` | `0.1` | H1 神经元的权重。 |
| `Q1`, `Q2`, `Q3` | `0.1` | H4 神经元的权重。 |
| `K1`, `K2`, `K3` | `0.1` | D1 神经元的权重。 |
| `EnableDetailedLog` | `false` | 输出模拟原 EA 日志的详细信息。 |

## 风险管理

- 策略会自动识别 3/5 位报价并转换点值，在仓位方向发生变化后立即根据 `StopLossPips` 与 `TakeProfitPips` 计算价格级别。
- 拖尾止损只有在价格突破 `TrailingStopPips + TrailingStepPips` 后才会启动，并且只有当改进幅度大于 `TrailingStepPips` 才会再次上移/下移。
- 因为高级 API 没有服务器端止损/止盈订单，所以所有离场都使用 `ClosePosition()` 市价完成。

## 说明

- 原始 EA 中对冻结区/最小止损距离的检查在 StockSharp 中不可用，本策略通过点值换算以及 `VolumeStep`、`VolumeMin`、`VolumeMax` 对交易量进行归一化。
- 风险百分比模式根据投资组合价值与当前价格估算交易手数，能够保持与 MetaTrader `CheckOpenLong/Short` 类似的行为，但不依赖券商的保证金计算。
- `EnableDetailedLog` 可用于调试，生成与 `InpPrintLog` 接近的逐步日志。
