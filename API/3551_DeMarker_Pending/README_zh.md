# DeMarker Pending 策略

## 概述
该策略将 MetaTrader 专家顾问 "DeMarker Pending 2.5" 移植到 StockSharp 平台。策略在可配置的周期上计算 DeMarker 指标，当指标突破设定的上下阈值时，按照突破方向创建挂单。挂单价格通过设置点差偏移，既可以是顺势突破的 Stop 单，也可以是回撤入场的 Limit 单，并可选地按照时间窗口和有效期管理等待中的挂单。

## 交易逻辑
- 订阅指定时间框架的蜡烛，并使用 `DemarkerPeriod` 作为周期计算 DeMarker 指标。
- 比较当前完成蜡烛与前一根蜡烛的 DeMarker 数值，检测是否跨越下阈值 `DemarkerLowerLevel` 或上阈值 `DemarkerUpperLevel`。
- 当指标自下方穿越下阈值时记录做多信号；当指标自上方跌破上阈值时记录做空信号。
- 根据 `Mode` 选择 Stop 模式（突破方向加上偏移）或 Limit 模式（回撤方向减去偏移），将挂单价格设为 `Close ± PendingIndentPoints * PriceStep`。
- 在提交挂单时，同时设置距入场价 `StopLossPoints` 和 `TakeProfitPoints` 点的止损、止盈水平。
- 按照 `ReplacePreviousPending` 和 `SinglePendingOnly` 的设置决定是否在下达新挂单前取消旧挂单或限制挂单数量。
- 按照 `PendingExpirationMinutes` 指定的分钟数为挂单设置有效期，到期后自动撤单。
- 开启 `UseTimeWindow` 时，仅在指定的交易时间窗口内响应信号，每根蜡烛最多生成一个方向上的新挂单。

## 订单管理
- 所有入场均通过 `BuyStop`、`SellStop`、`BuyLimit` 或 `SellLimit` 形式的挂单完成。
- 挂单在注册时即附带止损和止盈价格，确保成交后立即受到保护。
- 当挂单到期、被新的信号替换、或订单状态变为非激活（成交、取消、拒绝）时自动撤销。

## 参数
| 参数 | 说明 |
|------|------|
| `Volume` | 下单手数（Lots）。 |
| `StopLossPoints` | 入场价与止损之间的点数距离。 |
| `TakeProfitPoints` | 入场价与止盈之间的点数距离。 |
| `PendingIndentPoints` | 市场价与挂单价之间的偏移点数。 |
| `PendingExpirationMinutes` | 挂单的有效期（分钟，0 表示不过期）。 |
| `Mode` | 挂单类型（Stop 突破或 Limit 回撤）。 |
| `SinglePendingOnly` | 是否限制同时仅保留一张挂单。 |
| `ReplacePreviousPending` | 新挂单前是否自动取消现有挂单。 |
| `DemarkerPeriod` | DeMarker 指标的观察周期。 |
| `DemarkerUpperLevel` | 触发做空信号的 DeMarker 阈值。 |
| `DemarkerLowerLevel` | 触发做多信号的 DeMarker 阈值。 |
| `CandleType` | 指标计算所用的蜡烛时间框架。 |
| `UseTimeWindow` | 是否启用日内时间过滤。 |
| `StartTime` | 允许交易窗口的开始时间。 |
| `EndTime` | 允许交易窗口的结束时间。 |

## 说明
- 原版 EA 包含复杂的资金管理与移动止损模块。本移植版本保留信号和挂单处理逻辑，将仓位大小简化为固定的 `Volume` 参数。
- StockSharp 在注册挂单时同时发送止损/止盈价格，请确认经纪商支持对 Stop/Limit 挂单附加保护价格。
- 使用前请根据品种的 `PriceStep` 校准所有点数参数，确保 `PendingIndentPoints`、`StopLossPoints`、`TakeProfitPoints` 满足交易所或经纪商的最小距离要求。
