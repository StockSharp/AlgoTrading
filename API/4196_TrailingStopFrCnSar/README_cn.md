# Trailing Stop FrCnSar 策略

## 概述
Trailing Stop FrCnSar 策略移植了 MetaTrader 套件中的 **TrailingStopFrCnSARen_v4.mq4** 和 **OrderBalansEN_v3_4.mq4**。原始脚本通过多种方法（最近蜡烛极值、分形、价格“速度”或 Parabolic SAR）调整已有订单的止损，并在图表上展示账户信息。StockSharp 版本在净头寸模式下重写了全部逻辑，同时提供可选的日志输出，以文本方式复刻 OrderBalans 指标的监控面板。

该策略不会自动开仓。它持续监控 `Strategy.Security` 的净头寸，根据所选模式与过滤条件计算理想的移动止损，并在价格触及该水平时通过市价单平仓。由于 StockSharp 按净持仓统计，因此所有计算都会作用于整体仓位，而非单独的 MetaTrader 订单。

## 交易逻辑
1. 订阅参数 `CandleType` 指定的蜡烛序列，只处理收盘后的蜡烛，避免提前移动止损。
2. 维护蜡烛高低价的短期缓冲区，在不调用受限函数的情况下获取最近极值或分形。
3. 当选择速度模式时，根据 `VelocityPeriod` 计算以点数衡量的收盘价平均变化，模拟 MetaTrader 的 Velocity 指标。
4. 每根完成的蜡烛都会计算新的候选止损价：
   - 最近蜡烛的最低/最高价减去或加上 `DeltaPoints`。
   - 最近确认的五根分形价格加上偏移。
   - 当前收盘价减去/加上按速度调整后的距离。
   - 当前 Parabolic SAR 值再叠加偏移。
   - 固定点差模式直接使用常数距离。
5. 使用资金管理过滤器验证候选价格：是否要求已有止损、是否只在盈利后才移动、达到盈亏平衡后是否停止更新、是否参考平均建仓价。
6. 只有当候选价相对现有止损至少改善 `StepPoints` 点时才会更新。
7. 当蜡烛突破存储的止损价（多头看最低价，空头看最高价）且允许交易时，通过市价单平掉净头寸。
8. 如果启用 `LogOrderSummary`，在日志中输出余额、仓位、开仓价、当前止损以及浮动盈亏，模拟 OrderBalans 指标的提示面板。

## 移动止损模式
- **Candle（蜡烛）**：跟随最近的显著蜡烛极值，可通过 `DeltaPoints` 设置缓冲。
- **Fractal（分形）**：使用最近一个五柱分形，行为与原始 EA 一致。
- **Velocity（速度）**：根据 `VelocityPeriod` 平滑的收盘价变化调整止损，`VelocityMultiplier` 越大，止损越紧。
- **Parabolic（抛物线 SAR）**：跟随 StockSharp 的 Parabolic SAR 指标，并支持步长和最大加速度。
- **FixedPoints（固定点差）**：始终保持固定距离，对应原脚本的“>4 pips” 情形。
- **Off（关闭）**：禁用移动止损，仅保留当前值。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Mode` | `TrailingStopMode` | `Candle` | 当前使用的移动止损算法。 |
| `CandleType` | `DataType` | 15 分钟蜡烛 | 用于分析和计算止损的时间框架。 |
| `DeltaPoints` | `int` | `0` | 在原始止损价基础上增加或减少的点数偏移。 |
| `StepPoints` | `int` | `0` | 更新止损前所需的最小改善幅度。 |
| `FixedDistancePoints` | `int` | `50` | 固定点差模式使用的距离。 |
| `TrailOnlyProfit` | `bool` | `true` | 仅在止损能带来正收益时才开始移动。 |
| `TrailOnlyBreakEven` | `bool` | `false` | 止损达到盈亏平衡后不再继续上移。 |
| `RequireExistingStop` | `bool` | `false` | 未生成初始止损前忽略所有更新。 |
| `UseGeneralBreakEven` | `bool` | `false` | 按净头寸的平均建仓价判定是否盈利，与原脚本的 `TProfit` 函数对应。 |
| `VelocityPeriod` | `int` | `30` | 计算速度时使用的收盘价数量。 |
| `VelocityMultiplier` | `decimal` | `1` | 调整速度对止损距离影响的系数。 |
| `ParabolicStep` | `decimal` | `0.02` | Parabolic SAR 的加速步长。 |
| `ParabolicMaximum` | `decimal` | `0.2` | Parabolic SAR 的最大加速度。 |
| `LogOrderSummary` | `bool` | `true` | 是否记录类似 OrderBalans 的账户摘要。 |
| `TradeVolume` | `decimal` | `1` | 平仓时默认使用的数量。 |

## 与原脚本的差异
- StockSharp 采用净头寸模式，不再区分单独的订单编号。移动止损直接作用于总仓位。
- 移除了魔术号和多品种过滤器。策略仅跟踪 `Strategy.Security`，假设仓位管理由外部完成。
- Velocity 模式使用收盘价变化的平均值来近似原自定义指标，数值会非常接近但不完全一致。
- 所有图表对象和标签改为日志输出。`LogOrderSummary` 参数提供与 OrderBalans 指标相似的文本统计。
- MetaTrader 的 `OrderModify` 功能替换为 StockSharp 的市价平仓助手方法。

## 使用建议
- 将策略添加到图表可以直观观察不同模式的效果；启用图表区域后也可观察 Parabolic SAR 的点位。
- `DeltaPoints` 与 `StepPoints` 需要结合品种最小跳动单位设置，策略会自动乘以 `PriceStep` 或 `MinPriceStep`。
- 如果希望保持原脚本风格，应保持 `TrailOnlyProfit=true`，这样止损只会在盈利时启动。
- 批量运行时可关闭 `LogOrderSummary`，避免输出过多日志。
- 在速度模式下调节 `VelocityMultiplier` 可以改变止损的紧凑程度，数值越大越灵敏。

## 指标
- Parabolic SAR (`ParabolicSar`)
- 蜡烛高低价滚动缓冲区（用于分形和蜡烛模式）
- 可选的收盘价速度均值（用于 Velocity 模式）
