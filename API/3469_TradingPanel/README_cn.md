# TradingPanelStrategy

## 概述
`TradingPanelStrategy` 是 MetaTrader 4 专家顾问 **EA_TradingPanel** 的 StockSharp 版本。原脚本提供了一个手动面板，交易者先设置批量下单的数量、每笔手数以及止损/止盈距离，然后点击 **BUY** 或 **SELL** 按钮。移植到 StockSharp 后，操作员只需在参数中指定 `Direction`，策略就会在下一根完整蜡烛关闭时按照设定方向批量发送市价单，并立即把方向重置为 `None`。

逻辑保持轻量，方便与外部信号或人工干预组合使用。所有订单都会继承可选的止损和止盈，距离以点（pip）表示，完全对应原 MQL 面板的风险控制。

## 工作流程
1. 策略启动时根据 `Security.PriceStep` 计算 pip 大小。对于 1/3/5 位报价的外汇品种，会额外乘以 10，与 MetaTrader 将 point 转换为 pip 的方式一致。
2. 若止损或止盈距离大于零，则调用 `StartProtection`，并使用市价单管理离场。
3. 订阅 `CandleType` 参数指定的蜡烛序列。每当有蜡烛收盘，就检查当前的 `Direction`。
4. 如果指定了方向且允许交易，策略会按照 `NumberOfOrders` 的数量、`OrderVolume` 的手数连续发送市价单。
5. 批量下单完成后写入日志，并把 `Direction` 自动恢复为 `None`，等待下一次人工触发。

通过这种设计，策略在两次执行之间保持无状态。操作者可以在需要时把 `Direction` 改成 `Buy` 或 `Sell`，下一根完成的蜡烛就会触发新的订单批次，避免在未完成的数据上行动。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| ---- | ---- | ------ | ---- |
| `NumberOfOrders` | `int` | `1` | 下一批要发送的市价单数量。 |
| `OrderVolume` | `decimal` | `0.01` | 每一张市价单的交易量。 |
| `StopLossPips` | `decimal` | `2` | 以 pip 表示的止损距离，会根据品种元数据转换为绝对价格。设为 `0` 表示不使用。 |
| `TakeProfitPips` | `decimal` | `10` | 以 pip 表示的止盈距离。设为 `0` 表示不使用。 |
| `Direction` | `TradeDirection` | `None` | 下一次执行的方向，发送完毕后会自动重置为 `None`。 |
| `CandleType` | `DataType` | `TimeFrameCandle(1m)` | 用于触发执行的蜡烛类型。 |

## 说明
- 运行前需要为 `Security` 配置正确的 `PriceStep`（以及 `Decimals`），否则 pip 转换将退化为常数 `1`。
- `StartProtection` 使用市价单离场，以模拟原面板在触发止损/止盈时的处理方式。
- 由于执行发生在完整蜡烛上，操作者可以在蜡烛收盘前更新 `Direction`，从而把外部分析或手动判断与批量下单同步。
