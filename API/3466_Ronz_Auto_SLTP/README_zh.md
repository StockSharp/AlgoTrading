# Ronz Auto SLTP 策略

## 概述

**Ronz Auto SLTP 策略** 是 MetaTrader 5 工具 *Ronz Auto SLTP* 的 C# 版本。该策略充当交易管理器，能够自动为已有仓位添加止损和止盈、在盈利达到阈值后锁定利润，并根据原始 EA 的三种模式启动跟踪止损。移植版基于 StockSharp 的高级 API，可选择只管理当前品种或整个投资组合。

主要功能：

- 通过 `UseServerStops` 参数在真实服务器订单与客户端虚拟保护之间切换。
- 使用 MetaTrader 风格的点值配置初始止损和止盈距离，并自动考虑交易商的最小止损距离限制。
- 在 `LockProfitAfterPips` 达到后，将止损移动到盈利方向，从而锁定 `ProfitLockPips` 的利润。
- 支持 `Classic`、`StepDistance`、`StepByStep` 三种跟踪止损模式，逻辑与原始脚本保持一致。
- 当 `ManageAllSecurities` 为真时，自动订阅投资组合中所有持仓品种的 Level1 数据并逐个管理。
- 在虚拟模式下触发平仓时，可选地通过日志输出提醒信息。

## 参数说明

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `ManageAllSecurities` | `true` | 监控并管理投资组合中所有持仓。关闭后仅处理当前策略的品种。 |
| `TakeProfitPips` | `550` | 相对开仓价的止盈距离（点），会额外加上经纪商要求的最小止损距离。 |
| `StopLossPips` | `350` | 相对开仓价的止损距离（点），会额外加上经纪商要求的最小止损距离。 |
| `UseServerStops` | `true` | 启用后在交易服务器上挂出真实的止损/止盈单；关闭后仅在本地监控并通过市价单平仓。 |
| `EnableLockProfit` | `true` | 是否启用盈利锁定逻辑。 |
| `LockProfitAfterPips` | `100` | 当前浮盈达到多少点后开始锁定利润。设为 0 将跳过锁定阶段直接启用跟踪止损。 |
| `ProfitLockPips` | `60` | 锁定利润的点数，即止损被移动到距开仓价该距离的位置。 |
| `TrailingStopMode` | `Classic` | 跟踪止损模式，可选 `None`、`Classic`、`StepDistance`、`StepByStep`。 |
| `TrailingStopPips` | `50` | 跟踪止损保持的距离（点），是经典与阶梯模式的主要参数。 |
| `TrailingStepPips` | `10` | 阶梯模式每次推进的步长（点），对经典模式无效。 |
| `EnableAlerts` | `false` | 虚拟模式平仓时是否在日志中输出提示。 |

## 工作流程

1. **初始化保护**：当检测到新的净仓位时，按照开仓价、点值以及最小止损距离计算初始止损与止盈。若经纪商要求的最小距离更大，系统会自动放大目标。
2. **锁定利润**：若启用了 `EnableLockProfit` 且浮盈超过 `LockProfitAfterPips`，止损将被移动到开仓价外 `ProfitLockPips` 点的位置，确保保留部分利润。
3. **跟踪止损**：
   - `Classic`：始终保持 `TrailingStopPips` 点的固定距离。
   - `StepDistance`：当盈利足够时，将距离缩小 `TrailingStepPips`，模拟 “Step Keep Distance” 行为。
   - `StepByStep`：每当价格向有利方向推进 `TrailingStopPips` 点，止损向前移动 `TrailingStepPips`。
   - 若 `LockProfitAfterPips` 为 0，跟踪止损立即生效；否则当浮盈超过 `LockProfitAfterPips + TrailingStopPips` 时启动。
4. **虚拟模式**：当 `UseServerStops` 为假时，不会在服务器注册保护性订单，而是在价格触发阈值时通过市价单主动平仓。若开启 `EnableAlerts`，会在日志中记录对应原因。
5. **多品种管理**：策略为每个品种维护独立的止损、止盈与跟踪状态，确保多头与空头交易互不干扰。只要投资组合中存在持仓，就会自动订阅该品种的 Level1 数据。

## 使用建议

- 启动策略前，请确保交易适配器能提供准确的买/卖报价，以便正确换算点值。
- 若经纪商限制止损距离过近，可适当增大 `StopLossPips` 或 `TakeProfitPips`，避免订单被拒绝。
- 在回测或无法挂出真实止损的环境下，建议关闭 `UseServerStops` 使用虚拟保护模式。
- 由于 StockSharp 使用净仓位模型，若同一品种存在多张对冲单，策略会以合并后的净仓位进行管理。

## 与原始 EA 的差异

- MetaTrader 可在对冲模式下针对每一张订单单独设置 SL/TP，本移植版基于 StockSharp 的净仓位模型进行管理。
- 原脚本的测试功能（在策略测试器中自动开立示例订单）未被保留。
- 提示信息通过 StockSharp 的日志系统输出，而不是 MetaTrader 弹窗。

