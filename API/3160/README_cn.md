# Cidomo 策略
[English](README.md) | [Русский](README_ru.md)

将 MetaTrader 5 的 "Cidomo" 专家顾问迁移到 StockSharp。策略在选定周期的每根完整 K 线上计算最近的价格区间，并在区间上方与下方分别挂出买入/卖出止损单。通过固定止损、止盈、阶梯式追踪止损以及两种仓位管理模式（固定手数或风险百分比）来控制风险。

## 运行流程

1. 每当 `CandleType` 周期的蜡烛收盘时，收集最近 `BarsCount` 根 K 线的最高价和最低价，构造短期价格通道。
2. 在 `最高价 + IndentPips` 位置挂出买入止损单，在 `最低价 - IndentPips` 位置挂出卖出止损单（均以点数配置并转换为绝对价格）。
3. 一旦任一止损单被触发，另一笔挂单会立刻取消，避免双向持仓。
4. 持仓期间策略会跟踪：
   - 初始止损 (`StopLossPips`) 与止盈 (`TakeProfitPips`) 价格。
   - 阶梯式追踪止损 (`TrailingStopPips` / `TrailingStepPips`)：只有在价格至少前进 `TrailingStop + TrailingStep` 点后才会上移/下移止损。
   - 当价格触及止损或止盈时，通过市价单离场，以模拟 MetaTrader 中的 `PositionModify` 行为。
5. 若启用 `UseTimeFilter`，新的挂单只会在 `StartHour:StartMinute` 附近 ±30 秒（服务器时间）内发送，与原始 EA 的时间过滤完全一致。

## 仓位管理

- **FixedVolume**：始终使用固定手数 `TradeVolume`。
- **RiskPercent**：根据止损距离计算下单数量，使得止损亏损约等于账户权益的 `RiskPercent`%。 计算结果会按照品种的 `VolumeStep` 取整，并受 `MinVolume` / `MaxVolume` 限制。

## 风险控制

- 止损与止盈价格在本地保存，一旦下一根蜡烛穿越相应价格立即以市价平仓。
- 追踪止损只会向有利方向移动，并遵守最小步长，避免频繁微调。
- 当未设置止损 (`StopLossPips = 0`) 时，风险百分比模式会自动退回到固定手数 `TradeVolume`。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H4` | 构建突破区间所使用的周期。 |
| `BarsCount` | `int` | `15` | 统计最高/最低价时考虑的已完成蜡烛数量。 |
| `IndentPips` | `decimal` | `3` | 在区间边界外额外添加的点数距离。 |
| `StopLossPips` | `decimal` | `50` | 止损距离（点）。`0` 表示不设止损。 |
| `TakeProfitPips` | `decimal` | `50` | 止盈距离（点）。`0` 表示不设止盈。 |
| `TrailingStopPips` | `decimal` | `35` | 追踪止损的基础距离（点）。`0` 表示禁用追踪止损。 |
| `TrailingStepPips` | `decimal` | `5` | 每次收紧追踪止损所需的额外盈利（点）。 |
| `MoneyManagement` | `CidomoMoneyManagementMode` | `RiskPercent` | 选择固定手数或风险百分比两种下单方式。 |
| `RiskPercent` | `decimal` | `1` | 在风险百分比模式下，每笔交易允许损失的权益百分比。 |
| `TradeVolume` | `decimal` | `0.1` | 固定下单手数；当无法计算风险手数时也会采用该值。 |
| `UseTimeFilter` | `bool` | `false` | 是否启用 ±30 秒的时间过滤。 |
| `StartHour` | `int` | `9` | 时间过滤中心的小时（0-23）。 |
| `StartMinute` | `int` | `58` | 时间过滤中心的分钟（0-59）。 |

## 说明

- 所有以点数表示的参数都会根据报价的精度自动调整（对于 3/5 位小数的品种会乘以 10），与原始 EA 一致。
- 由于本移植版本在客户端监控止损/止盈，请确保策略持续在线，以便在价格触发时及时发出市价退出指令。
