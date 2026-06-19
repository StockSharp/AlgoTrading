# Ten Pips Opposite Last N Hour Trend 策略

## 概述

该策略是 MetaTrader 专家 **10pipsOnceADayOppositeLastNHourTrend** 的移植版本。它每天只在一个指定的小时交易一次，并且刻意反向跟随最近 *N* 根小时 K 线的价格变化。原始脚本面向 5 位小数的外汇品种，C# 版本会根据 `PriceStep` 和小数位数自动换算点值，因此同样适用于 3 位小数的品种。

到达交易时间后，策略会比较 `HoursToCheckTrend` 小时之前的收盘价与最近一根已完成小时 K 线的收盘价：

- 如果较早的收盘价 **更高**，说明价格近段时间下跌，于是开出 **多头** 仓位。
- 否则价格上涨，则开出 **空头** 仓位。

仓位可以被保护性止损/止盈、持仓超时或超出交易时段等条件关闭。

## 资金管理

头寸规模完全复制原始 EA 的“阶梯式”马丁格尔逻辑：

1. 基础手数来自 `FixedVolume`。当它为 0 时，按照 `Portfolio.CurrentValue * MaximumRisk / 1000` 计算，并四舍五入到 0.1 手。
2. 结果会受到 `MinimumVolume`、`MaximumVolume`、交易所的最小/最大手数以及软限制 `Portfolio.CurrentValue / 1000` 的约束。
3. 策略会保存最近五笔平仓结果。准备下一次进场时，按从近到远的顺序查找第一次出现的亏损，并使用 `FirstMultiplier` … `FifthMultiplier` 中对应的倍数调整手数，完全模拟 MQL 中层层嵌套的 `OrderSelect` 判断。

## 风险控制

- `StopLossPips`、`TakeProfitPips`、`TrailingStopPips` 以点为单位。移植时按照外汇常用的 3/5 位小数规则自动放大 10 倍。
- 多、空两侧的跟踪止损采用同一套逻辑。原始 EA 在空头方向存在符号错误导致永远不会移动止损，C# 版本修复了这一问题。
- `OrderMaxAge` 用于平掉持仓时间超过阈值（默认 21 小时）的订单。
- 如果当前小时不在允许列表内，策略会立即平仓并等待下一次机会。
- `MaxOrders` 确保在有持仓或挂单时不会重复进场。

## 工作流程

1. 订阅 `CandleType` 指定的 K 线（默认 1 小时）。
2. 将每根完成 K 线的收盘价写入滚动缓冲区。
3. 在达到设定交易小时的第一根完成 K 线上：
   - 检查连接状态并确认没有持仓。
   - 确保历史缓冲区中至少包含 `HoursToCheckTrend` 根 K 线。
   - 比较当前收盘价与 `HoursToCheckTrend` 小时前的收盘价，得出买卖方向。
   - 根据资金管理规则计算手数并发送市价单。
4. 持仓期间：
   - 根据 K 线的最高价/最低价检查止损、止盈和跟踪止损是否触发。
   - 创出新高/新低时，更新跟踪止损的位置。
   - 记录建仓时间，用于判断 `OrderMaxAge`。
   - 平仓时保存盈亏结果供下一次手数调整使用。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `FixedVolume` | 固定下单手数。设为 `0` 时改用风险百分比。 | `0.1` |
| `MinimumVolume` | 下单量下限。 | `0.1` |
| `MaximumVolume` | 下单量上限。 | `5` |
| `MaximumRisk` | 当 `FixedVolume = 0` 时的风险比例。 | `0.05` |
| `MaxOrders` | 允许同时存在的订单/仓位数量。 | `1` |
| `TradingHour` | 允许进场的小时（0–23）。 | `7` |
| `HoursToCheckTrend` | 回溯的小时数量。 | `30` |
| `OrderMaxAge` | 持仓最长时间。 | `21 小时` |
| `StopLossPips` | 止损距离（点）。 | `50` |
| `TakeProfitPips` | 止盈距离（点）。 | `10` |
| `TrailingStopPips` | 跟踪止损距离（点）。 | `0`（关闭） |
| `FirstMultiplier` … `FifthMultiplier` | 在最近第 1…5 笔亏损出现时的手数乘数。 | `4`, `2`, `5`, `5`, `1` |
| `CandleType` | 计算所用的 K 线类型。 | `1 小时` |

## 与 MQL 版本的差异

- 马丁格尔、持仓时间和交易时间窗口等核心逻辑保持一致，唯一的改动是修复了空头方向的跟踪止损。
- 保护性止损/止盈在下一根完成 K 线上以市价平仓，这与原专家的实际效果一致。
- 账户权益读取自 `Portfolio.CurrentValue`。若连接器未提供该字段，则退回到策略的基础 `Volume`（默认为 1）。
- 允许的交易小时列表为 `0…23`。如需限制具体工作日，可在构造函数中修改 `_tradingDayHours`。

## 使用建议

- 推荐在外汇小时级别数据上运行，确保点值换算符合预期。
- 请确认连接器提供 `VolumeStep`、`VolumeMin`、`VolumeMax` 等信息，以便策略能够调整手数。
- 为避免错过当日唯一的交易信号，应在目标交易小时之前启动策略。

