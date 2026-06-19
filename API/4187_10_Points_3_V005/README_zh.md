# Ten Points 3 v005 策略

## 概述
该策略将 MetaTrader 4 的「10points 3 v005」专家顾问移植到 StockSharp。通过比较 MACD 主线的当前值与前一值来判断做多或做空，当价格朝不利方向移动指定点数时追加马丁格尔订单。v005 版本额外提供交易时间窗口、账户权益保护以及独立关闭多头或空头循环的功能。

## 交易逻辑
- 根据 MACD 主线的斜率确定方向，`ReverseSignals` 可反向解释信号。
- 一旦有方向信号立即以市价开仓，之后每当价格相对当前仓位逆行 `EntryDistancePips` 点时加仓。
- 加仓数量按几何级数增长：默认使用 `MartingaleFactor`（当允许的最大交易数大于 12 时改用 `HighTradeFactor`）。订单数量会按照合约最小步长对齐，并限制在 100 手以内。
- 每次进场都会更新组合的止损和止盈。起始值由 `InitialStopPips` 与 `TakeProfitPips` 控制，盈利达到 `EntryDistancePips + TrailingStopPips` 后启用跟踪止损。
- 启用账户保护时，策略可以通过 `ReboundLock` 将目标对齐至最佳入场价，并在浮动盈利达到 `SecureProfit` 后平掉最近一次加仓。
- 权益保护包括：浮亏超过 `StopLossAmount`、权益超过 `ProfitTarget + ProfitBuffer` 或跌破 `StartProtectionLevel` 时立即平仓。
- 仅在 `OpenHour` 至 `CloseHour` 的时间段内交易，默认情况下周五全天停止交易（`CloseOnFriday`）。

## 资金管理
关闭 `UseMoneyManagement` 时使用固定手数 `LotSize`。开启后会根据当前组合价值和 `RiskPercent` 计算基准手数，`IsStandardAccount` 用于模拟标准账户或迷你账户的缩放。

## 参数
| 参数 | 说明 |
|------|------|
| `TakeProfitPips` | 每笔订单的止盈距离（点）。 |
| `LotSize` | 固定手数。 |
| `InitialStopPips` | 初始止损距离。 |
| `TrailingStopPips` | 触发后使用的跟踪止损距离。 |
| `MaxTrades` | 允许的最大马丁格尔订单数。 |
| `EntryDistancePips` | 追加仓位所需的逆向价格幅度。 |
| `SecureProfit` | 触发账户保护所需的浮动盈利（货币单位）。 |
| `UseAccountProtection` | 启用账户保护逻辑。 |
| `OrdersToProtect` | 受盈利保护的末尾订单数量。 |
| `ReverseSignals` | 反向解释 MACD 信号。 |
| `UseMoneyManagement` | 启用基于余额的仓位控制。 |
| `RiskPercent` | 资金管理使用的风险比例。 |
| `IsStandardAccount` | 标准账户或迷你账户缩放。 |
| `EurUsdPipValue` 等 | 计算浮动盈亏时使用的点值。 |
| `CandleType` | 生成信号的时间框架。 |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD 参数。 |
| `EnableLong`, `EnableShort` | 开启/关闭多头或空头循环。 |
| `OpenHour`, `CloseHour`, `MinuteToStop` | 交易时间设置。 |
| `StopLossProtection`, `StopLossAmount` | 账户级止损保护。 |
| `ProfitTargetEnabled`, `ProfitTarget`, `ProfitBuffer` | 权益目标锁定。 |
| `StartProtectionEnabled`, `StartProtectionLevel` | 权益下限保护。 |
| `ReboundLock` | 在保护模式下对齐目标价位。 |
| `MartingaleFactor`, `HighTradeFactor` | 马丁格尔倍数。 |
| `CloseOnFriday` | 是否在周五停止交易。 |

## 备注
- 策略采用 StockSharp 高级 API (`SubscribeCandles` + `BindEx`)，无需直接处理指标缓存。
- 所有保护措施都通过市价平仓来复现原始 EA 的行为。
- 在真实账户运行前，请确认参数、点值和允许手数符合交易品种及经纪商要求。
