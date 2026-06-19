# Starter 三重随机策略

本策略将 MetaTrader 专家顾问 **Starter.mq5** 移植到 StockSharp 高级 API。它在三个不同周期上计算移动平均线与
随机指标（快线、常规线、慢线），只有当所有过滤条件指向同一方向且价格位于每条平移后的移动平均线
适当一侧时才允许开仓。

## 交易逻辑

1. 订阅三个蜡烛序列：
   - **快速周期**（默认 `M5`）。
   - **常规周期**（默认 `M30`）。
   - **慢速周期**（默认 `H2`）。
2. 每个周期上构建一条可配置的移动平均线和一个随机指标，参数 `%K`、`%D` 与 `Slowing` 三者保持一致。
3. 慢速周期负责触发交易。当慢速蜡烛收盘时，比较各周期的最新指标：
   - 多头：所有随机指标满足 `%K > %D` 且 `%K < 50`，同时价格低于每条移动平均线（考虑 `MaShift` 平移）。
   - 空头：所有随机指标满足 `%K < %D` 且 `%K > 50`，同时价格高于每条移动平均线。
4. `ReverseSignals` 可将多空逻辑对调。`CloseOppositePositions = true` 时，出现反向信号会直接反手；否则在持有
   相反仓位时忽略信号。
5. 成交后在价格空间中跟踪止损与止盈。`TrailingStopPips` 与 `TrailingStepPips` 组合再现原策略：只有当浮动利润
   超过两者之和时才把止损向盈利方向平移 `TrailingStopPips`。
6. 资金管理完全复制 `lot/risk` 模式：`FixedLot` 使用固定手数，`RiskPercent` 按账户价值、风险百分比和止损距离
   动态计算下单量。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `StopLossPips` | `45` | 止损距离（点）。为 `0` 时禁用固定止损。 |
| `TakeProfitPips` | `105` | 止盈距离（点）。为 `0` 时禁用固定止盈。 |
| `TrailingStopPips` | `5` | 移动止损的基础偏移。 |
| `TrailingStepPips` | `5` | 启动移动止损所需的最小盈利位移。 |
| `MoneyMode` | `RiskPercent` | 资金管理模式：固定手数或按风险百分比。 |
| `MoneyValue` | `3` | `FixedLot` 模式下的手数，或 `RiskPercent` 模式下的风险百分比。 |
| `FastCandleType` | `M5` | 快速指标使用的蜡烛类型。 |
| `NormalCandleType` | `M30` | 常规指标使用的蜡烛类型。 |
| `SlowCandleType` | `H2` | 触发交易计算的蜡烛类型。 |
| `MaPeriod` | `20` | 移动平均线周期。 |
| `MaShift` | `1` | 移动平均线平移的柱数。 |
| `MaMethod` | `Simple` | 平滑方式：`Simple`、`Exponential`、`Smoothed`、`Weighted`。 |
| `MaPriceType` | `Close` | 计算移动平均线所用价格。 |
| `StochasticKPeriod` | `5` | 随机指标 `%K` 周期。 |
| `StochasticDPeriod` | `3` | `%D` 平滑周期。 |
| `StochasticSlowing` | `3` | `%K` 最终平滑系数。 |
| `ReverseSignals` | `false` | 是否对调多空条件。 |
| `CloseOppositePositions` | `false` | 是否在新信号出现时立即反手平仓。 |

## 资金管理

- `FixedLot`：每次下单量恒等于 `MoneyValue`。
- `RiskPercent`：风险金额为 `账户价值 * MoneyValue / 100`，下单量 = 风险金额 ÷ (`StopLossPips` × 点值)。若止损为
  0 或账户价值未知，则拒绝交易。

## 风险控制与移动止损

- 通过比较蜡烛高/低价模拟止损和止盈的触发，等价于 MetaTrader 中的保护性挂单。
- 只有当利润超过 `TrailingStopPips + TrailingStepPips` 点时，移动止损才会被更新，完全重现原始 EA 的逻辑。

## 多周期同步

所有指标仅在各自周期的收盘蜡烛上更新。慢速周期会等待三组指标全部形成，并读取最近的平移后移动平均
值，从而等效于 MetaTrader `iMA` 函数的 `shift` 参数，确保触发时机与原始 MQL 策略一致。
