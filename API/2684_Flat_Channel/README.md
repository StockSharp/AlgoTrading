# Flat Channel Strategy (2684)

This strategy is a C# conversion of the MetaTrader 5 expert advisor *Flat Channel (barabashkakvn's edition)*. It detects periods of low volatility (a "flat" channel) using the Standard Deviation indicator and places breakout stop orders at the channel boundaries. When price breaks out of the flat range the corresponding stop order is triggered, while the opposite order is cancelled to avoid being trapped on both sides of the market.

## Core logic

1. **Volatility filter** – the strategy subscribes to candles and calculates the median-price Standard Deviation. A flat phase is confirmed when the value keeps falling for at least `FlatBars` consecutive candles.
2. **Channel construction** – once the flat phase is confirmed, the highest high and lowest low of the flat range are tracked. The channel width must stay between `ChannelMinPips` and `ChannelMaxPips` (converted to price units via the instrument tick size).
3. **Entry orders** – while price trades inside the channel, the strategy places:
   - A buy stop at the channel high with stop-loss `2 × channel width` below the entry and take-profit `1 × channel width` above.
   - A sell stop at the channel low with the symmetric stop-loss/take-profit distances.
4. **Order lifetime** – pending stop orders expire after `OrderLifetimeSeconds`. When the timeout elapses they are cancelled and can be recreated if flat conditions still hold.
5. **Position management** – after an entry order is filled, the opposite stop order is cancelled and fresh protective orders (stop-loss and take-profit) are registered. Optional breakeven logic moves the stop-loss to the entry price once the price travels a Fibonacci fraction (`FiboTrail`) of the distance toward the take-profit target.
6. **Trading window** – the `UseTradingHours` filter restricts activity by weekday and by specific Monday/Friday hours, emulating the schedule controls from the original EA.

## Indicators

- **StandardDeviation** (median price, length = `StdDevPeriod`) – detects falling volatility.
- **DonchianChannels** (length = `FlatBars`) – provides the initial high/low bounds for the flat channel.

## Risk & money management

- `FixedVolume` defines the lot size when `UseMoneyManagement` is disabled.
- When `UseMoneyManagement` is enabled, the position size is estimated from `RiskPercent` of the current portfolio value divided by the stop-loss distance expressed in money using `PriceStep` and `StepPrice`.
- After a losing trade the next position uses `FixedVolume × 4`, replicating the original EA's recovery behaviour.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `UseTradingHours` | Enable or disable the weekday/hour schedule filter. |
| `TradeTuesday`, `TradeWednesday`, `TradeThursday` | Allow trading on individual mid-week days (Monday and Friday are always allowed but controlled by the hourly limits). |
| `MondayStartHour`, `FridayStopHour` | Start hour on Monday and cut-off hour on Friday (24h clock). |
| `UseMoneyManagement`, `RiskPercent`, `FixedVolume` | Money-management options described above. |
| `OrderLifetimeSeconds` | Expiration time for pending entry orders (0 = no expiration). |
| `StdDevPeriod`, `FlatBars` | Indicator settings controlling the flat-phase detection. |
| `ChannelMinPips`, `ChannelMaxPips` | Allowed channel width expressed in pips (converted using the instrument tick size). |
| `UseBreakeven`, `FiboTrail` | Enable breakeven logic and set the Fibonacci multiplier used to trigger the stop adjustment. |
| `CandleType` | Candle data type or timeframe used for calculations. |

## Notes

- The strategy expects symbols that expose `PriceStep` and `StepPrice` so the pip-based thresholds can be converted to actual prices.
- Pending orders are recreated only when volatility continues to fall. If volatility rises the flat state is reset and all entry orders are cancelled.
- Protective stop and take-profit orders are cancelled automatically when the position closes.

## Disclaimer

This example is provided for educational purposes only. Past performance of the original strategy does not guarantee future results. Thoroughly test and adjust the parameters before deploying to live markets.
