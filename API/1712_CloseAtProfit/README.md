# Close At Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy monitors the realized profit and loss of all trades executed by the strategy. When the accumulated profit exceeds a user defined threshold, it immediately closes any open position and optionally cancels active orders. The same behavior can be enabled for drawdown by setting a loss limit.

The strategy does not analyze indicators or price movement. Instead, it acts as a protective overlay that exits the market once a monetary target or stop level is reached. A simple candle subscription is used only to provide periodic checks of the current PnL value.

## Parameters

- **UseProfitToClose** – enable or disable closing by profit target. Default: `true`.
- **ProfitToClose** – profit value in currency units that triggers a full exit. Default: `20`.
- **UseLossToClose** – enable or disable closing by loss limit. Default: `false`.
- **LossToClose** – loss value in currency units that triggers a full exit when exceeded. Default: `100`.
- **ClosePendingOrders** – cancel all active orders when closing positions. Default: `true`.
- **CandleType** – type of candles used to trigger periodic checks. Default: `1` minute timeframe.

## Trading Logic

1. Subscribe to candles of the selected timeframe.
2. On each finished candle calculate current realized PnL.
3. If profit is greater than or equal to `ProfitToClose`, close the entire position and optionally cancel pending orders.
4. If loss monitoring is enabled and current PnL is less than or equal to `-LossToClose`, close the entire position and optionally cancel pending orders.

## Additional Notes

- The strategy closes only the position for the security it is attached to.
- Pending orders are canceled using the built-in `CancelActiveOrders` method.
- The logic can be combined with other entry strategies to implement profit taking or portfolio protection.

## Filters

- Category: Risk management
- Direction: Both
- Indicators: None
- Stops: Yes
- Complexity: Basic
- Timeframe: Any
- Seasonality: No
- Neural networks: No
- Divergence: No
- Risk level: Medium
