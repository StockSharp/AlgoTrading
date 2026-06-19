# EXP FIBO ZZ 策略

## 概述
EXP FIBO ZZ 策略是 MetaTrader 4 智能交易系统 `EXP_FIBO_ZZ_V1en` 的 C# 版本。它完整复刻了原始的区间突破思路：
跟踪最近确认的 ZigZag 通道，在摆动高点上方挂买入止损，在摆动低点下方挂卖出止损，并按照 Fibonacci
百分比附加止损和止盈委托。StockSharp 版本通过 `StrategyParam` 暴露所有输入参数，补充了大量有效性检查，
同时保留了原脚本的资金管理选项（基于账户余额/可用资金的风险计算及移动保本止损逻辑）。

## 交易逻辑
1. **数据准备**
   - 订阅配置的 `CandleType`（默认 1 分钟 K 线），并将数据传入长度为 `ZigZagDepth` 的 `Highest` 与 `Lowest`
     指标。
   - 轻量化的 ZigZag 检测器维护最近三个枢轴价。只有在满足以下条件时才登记新的枢轴：
     * 当前蜡烛的最高/最低价等于指标输出；
     * 距离上一个拐点至少经过 `ZigZagBackstep` 根 K 线；
     * 与当前枢轴价的偏差超过 `ZigZagDeviationPips`（以 MetaTrader point 为单位）。

2. **通道验证**
   - 当三个枢轴可用时，较早的两个枢轴定义当前通道。仅当通道高度位于 `MinCorridorPips` 与
     `MaxCorridorPips` 之间，并且最新枢轴落在通道内部且留有经纪商最小止损缓冲时才允许交易。
   - 若当前时间超出 `StartHour/StartMinute` 至 `StopHour/StopMinute` 的交易窗口，则立即撤销所有挂单。

3. **委托布置**
   - 买入/卖出止损价分别为通道上下沿加减 `EntryOffsetPips`。
   - 止损距离为 `通道高度 * FiboStopLoss / 100`，止盈距离沿用 MT4 公式 `通道高度 * (FiboTakeProfit / 100 - 1)`，
     若结果为负则视为 0。
   - 下单前计算交易量：当 `RiskPercent > 0` 时，取所选资金来源（`UseBalanceForRisk=true` 使用权益，否则使用权
     益减去已冻结保证金）乘以风险百分比，再除以参考价格。结果会根据交易所的 `VolumeStep`、`MinVolume`、
     `MaxVolume` 自动对齐。若缺少必要信息则退回到固定手数 `FixedVolume`。
   - 若目标价格或数量发生变化，则取消原有挂单并重新提交；否则保持原单。

4. **持仓管理**
   - 一旦成交持仓，立即撤销对侧挂单，并注册保护性委托：
     * 根据方向通过 `SellStop`/`BuyStop` 设置止损；
     * 若启用则通过 `SellLimit`/`BuyLimit` 设置止盈。
   - 可选的保本模块（`EnableBreakEven`）等同于原脚本的 `MovingInWL`：当浮动收益达到
     `BreakEvenTriggerPips` 后，将止损移动至开仓价加减 `BreakEvenOffsetPips`，确保至少锁定小幅利润，并避免重
     复移动。

5. **会话维护**
   - 离开交易窗口或持仓归零时，会撤销所有挂单与保护单。`OnStopped` 在策略停止时也会清理全部委托。

## 参数
| 名称 | 说明 | 默认值 | 备注 |
| --- | --- | --- | --- |
| `CandleType` | 构建 ZigZag 枢轴所用数据序列。 | `1m TimeFrame()` | 支持任意 StockSharp 蜡烛类型。 |
| `ZigZagDepth` | ZigZag 转折之间至少包含的 K 线数量。 | `12` | 对应 MT4 的 `ExtDepth`。 |
| `ZigZagDeviationPips` | 接受新枢轴所需的最小偏差（MetaTrader point）。 | `5` | 对应 `ExtDeviation`。 |
| `ZigZagBackstep` | 允许 ZigZag 反向之前的最小 K 线数量。 | `3` | 对应 `ExtBackstep`。 |
| `EntryOffsetPips` | 在通道上下沿外加减的点数，用于挂止损单。 | `5` | 对应 `n_pips`。 |
| `MinCorridorPips` | 通道高度下限。 | `20` | 对应 `Min_Corridor`。 |
| `MaxCorridorPips` | 通道高度上限。 | `100` | 对应 `Max_Corridor`。 |
| `FiboStopLoss` | 按通道高度计算止损距离的 Fibonacci 百分比。 | `61.8` | 对应 `Fibo_StopLoss`。 |
| `FiboTakeProfit` | 计算止盈目标的 Fibonacci 百分比。 | `161.8` | 对应 `Fibo_TakeProfit`。 |
| `StartHour`/`StartMinute` | 允许交易时段的起始时间。 | `00:01` | 时段外撤销全部挂单。 |
| `StopHour`/`StopMinute` | 允许交易时段的结束时间。 | `23:59` | 支持跨越午夜的会话。 |
| `UseBalanceForRisk` | 选择使用权益（`true`）或可用资金（`false`）计算风险。 | `true` | 对应 `Choice_method`。 |
| `RiskPercent` | 每笔交易使用的风险百分比。 | `1` | 设为 `0` 可关闭风险控制。 |
| `FixedVolume` | 无法使用风险计算时的固定手数。 | `0.1` | 对应 `Lots`。 |
| `EnableBreakEven` | 是否启用移动保本止损。 | `true` | 对应 `MovingInWL`。 |
| `BreakEvenTriggerPips` | 触发保本的盈利点数。 | `13` | 对应 `LevelProfit`。 |
| `BreakEvenOffsetPips` | 移动止损时在开仓价外加减的点数。 | `2` | 对应 `LevelWLoss`。 |
| `DrawCorridorLevels` | 是否在图表上绘制当前通道。 | `false` | 对应原脚本的线段开关。 |

## 实现要点
- 点值计算遵循 MetaTrader 规则：对于 3 位或 5 位小数的外汇品种将 `PriceStep` 乘以 10。
- 委托价格与数量均基于交易所元数据（`PriceStep`、`VolumeStep`、`MinVolume`、`MaxVolume`）自动对齐。
- 当缺少投资组合或价格信息时，风险控制会安全地退回到固定手数，避免策略停摆。
- 保本逻辑仅在满足条件时取消并重新登记止损，不会把止损移到开仓价外侧。
- 启用 `DrawCorridorLevels` 时，策略会在图表上绘制当前通道上下沿，便于直观核对区间。

## 与 MetaTrader 版本的差异
- 删除了 MT4 中的图形对象、提示音与注释显示，改用 StockSharp 的日志与图表绘制功能。
- 由于不同券商的 `MarketInfo` 保证金参数不可通用，风险计算改为基于投资组合权益与最近价格。
- 委托处理采用 StockSharp 高层 API（`BuyStop`、`SellStop`、`SellLimit`、`BuyLimit`），避免手动管理订单票据，
  行为与原脚本保持一致。
- ZigZag 检测利用内置指标实现深度/偏差/回溯逻辑，以适配 StockSharp 的流式蜡烛模型。
