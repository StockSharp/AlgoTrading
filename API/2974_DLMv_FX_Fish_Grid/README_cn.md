# DLMv FX Fish 网格策略

## 概述

**DLMv FX Fish 网格策略** 复刻了原始 MetaTrader 专家顾问的核心思想。策略依赖 “FX Fish 2MA” 振荡指标：先对选定价格应用 Fisher 变换，再用移动平均线平滑，然后在振荡值跨越平滑曲线并位于零轴适当一侧时建立仓位。持仓管理保留了 EA 的网格特性——根据设定间距分批加仓，可选的挂单自动布设，并配合完整的风险控制。

## 交易逻辑

1. **指标计算**
   - 在 `CalculatePeriod` 根 K 线内获取最高价和最低价作为区间。
   - 对 `AppliedPrice` 选定的价格执行 Fisher 变换，并保留原脚本使用的 0.67 平滑系数。
   - 对 Fisher 数值按 `MaPeriod` 计算简单移动平均，作为信号基线。
2. **信号判定**
   - **做多信号**：当前和上一根 Fisher 值均小于 0，且当前值上穿移动平均线（上一值在基线之下，本值在基线之上）。
   - **做空信号**：当前和上一根 Fisher 值均大于 0，且当前值下穿移动平均线（上一值在基线之上，本值在基线之下）。
   - 若启用 `ReverseSignals`，上述条件将互换方向。
3. **下单行为**
   - 新信号出现时可按 `CloseOpposite` 设置决定是否先平掉反向持仓。
   - 同向加仓数量受 `MaxTrades` 限制，且每次加仓必须满足距离最新成交价不少于 `DistancePips`。
   - 开启 `SetLimitOrders` 后，会在指定距离自动挂出限价单，构建与原 EA 一致的网格结构。
4. **风险控制**
   - `StopLossPips`、`TakeProfitPips`、`TrailingStopPips` 均通过 `StartProtection` 自动转换为价格差，形成止损、止盈与移动止损。
   - `TimeLiveSeconds` 限制持仓最长存续时间，超时将平仓并撤单。
   - 当 `TradeOnFriday = false` 时，周五到来会立即停止交易并清空持仓及挂单。

## 参数说明

| 参数 | 说明 |
|------|------|
| `OrderVolume` | 每次入场的手数。 |
| `StopLossPips` | 止损距离（点），0 表示关闭。 |
| `TakeProfitPips` | 止盈距离（点），0 表示关闭。 |
| `TrailingStopPips` | 移动止损距离，0 表示关闭。 |
| `TrailingStepPips` | 移动止损的跟进步长。 |
| `MaxTrades` | 同方向最多持仓数量，0 表示不限制。 |
| `DistancePips` | 连续入场之间的最小间距，同时也是网格挂单的偏移距离。 |
| `TradeOnFriday` | 是否允许周五交易。设为 `false` 时会在周五清仓并停用策略。 |
| `TimeLiveSeconds` | 持仓允许的最长时间（秒）。 |
| `ReverseSignals` | 是否反转多空条件。 |
| `SetLimitOrders` | 是否自动布设限价网格单。 |
| `CloseOpposite` | 新信号出现时是否先平掉反向仓位。 |
| `CalculatePeriod` | Fisher 变换的区间长度。 |
| `MaPeriod` | Fisher 数值平滑的均线长度。 |
| `AppliedPrice` | Fisher 变换使用的价格类型（收盘价、开盘价、最高、最低、中值、典型价、加权价）。 |
| `CandleType` | 策略处理的 K 线类型/周期。 |

## 备注

- 止损、止盈、移动止损都会按 `Security.PriceStep * 10` 将点值转换为绝对价格，与原 MT5 脚本在 5 位报价中的点值处理方式一致。
- 信号翻转、暂停交易或到达寿命限制时，挂出的限价单会被自动撤销。
- 为避免重复读取历史值，策略缓存上一根 Fisher 和均线结果，从而准确检测交叉点。
