# Time Trader 策略

该策略在预设时间发送市价单，并使用固定的止损和止盈进行保护。

## 交易规则

- 当当前时间达到 `Trade Hour:Trade Minute:Trade Second` 时，策略在本次运行中仅触发一次。
- 若启用 `Allow Buy`，则按 `Volume` 开多单。
- 若启用 `Allow Sell`，则按相同 `Volume` 开空单。
- 通过 `StartProtection` 管理保护单，止损和止盈以点数表示。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| `Volume` | 下单数量。 |
| `Take Profit (ticks)` | 距离入场价的止盈点数。 |
| `Stop Loss (ticks)` | 距离入场价的止损点数。 |
| `Allow Buy` | 允许做多。 |
| `Allow Sell` | 允许做空。 |
| `Trade Hour` | 入场的小时 (0-23)。 |
| `Trade Minute` | 入场的分钟 (0-59)。 |
| `Trade Second` | 入场的秒钟 (0-59)。 |
| `Candle Type` | 用于跟踪时间的K线类型，默认使用1秒K线。 |

## 备注

策略在一次运行中仅开仓一次。若需再次交易，请重启策略或调整入场时间。
