# Huge Income Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 4 expert advisor "Huge Income". The original robot looks for intraday moves that stretch away from the daily open and enters a single position in the direction of the breakout. The StockSharp version keeps the same idea by rebuilding the daily high/low range from intraday candles, opening only one position at a time and forcing an exit just before the configured market close.

## Data and environment
- **Instruments**: Any symbol that provides a reliable price step (`PriceStep`). The logic was designed for forex pairs but works on other instruments after adjusting the pip parameters.
- **Timeframe**: By default the strategy subscribes to 15-minute candles to reconstruct the daily open, high and low. You can switch to a different candle type if your data source offers better resolution.
- **Sessions**: The chart time is expected to follow the broker/server clock exactly like the MetaTrader script. Set the cutoff hours according to that timezone.

## Trading logic
1. Rebuild the current day's statistics whenever a new candle arrives. The first candle of the day provides the open price and initializes the running high/low.
2. Only one position (long or short) is allowed at any moment. Pending orders are not used; the strategy relies on market orders.
3. **Long setup**:
   - Current close is above the daily open.
   - The distance between the open and the current day's low is greater than `MinimumRangePips` (converted to price units through `PriceStep`).
   - The current hour is strictly less than `BuyCutoffHour`.
4. **Short setup**:
   - Current close is below the daily open.
   - The distance between the current day's high and the open is greater than `MinimumRangePips`.
   - The current hour is strictly less than `SellCutoffHour`.
5. When either setup is met, the strategy sends a market order with size `TradeVolume` and stops evaluating new entries until the position is flat again.
6. After the `MarketCloseHour` is reached, any open position is closed with a market order. This mirrors the MetaTrader logic that liquidates trades near the weekend close.

## Risk and money management
- `TradeVolume` is the fixed order size. There is no averaging or martingale behaviour in the original script, therefore the StockSharp port keeps a constant volume.
- There are no explicit stop-loss or take-profit levels. The expert advisor relies on the daily range filter and the forced close near the session end to control risk. You can extend the strategy by adding stops or trailing logic if needed.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Position size used when sending `BuyMarket` or `SellMarket` orders. |
| `MinimumRangePips` | Minimum distance (in pips) between the daily open and the opposite extreme before a trade is allowed. Converted to an absolute price difference using `Security.PriceStep`. |
| `BuyCutoffHour` | Last hour (0–23) when new long entries may be opened. The comparison is strict (`currentHour < BuyCutoffHour`). |
| `SellCutoffHour` | Last hour (0–23) when new short entries may be opened. |
| `MarketCloseHour` | Hour of the day when all open positions are liquidated. Set it to 23 to match the original EA closing behaviour on Fridays. |
| `CandleType` | Timeframe used to subscribe to candles and reconstruct daily statistics. |

## Differences from the MT4 version
- StockSharp receives candle data instead of individual ticks. If your broker's MetaTrader feed relied on tick-by-tick updates, choose a sufficiently small candle interval to emulate the same responsiveness.
- The `MinimumRangePips` filter is automatically disabled when the instrument lacks a `PriceStep`. In that case every breakout above/below the open is accepted.
- All trades are executed with market orders and immediately flattened at `MarketCloseHour`, replicating the `OrderClose` loop of the original code without pending orders.

## Usage tips
- Adjust the candle timeframe to match your preferred execution speed. Shorter candles track the daily high/low more accurately but require more data.
- Review the instrument's trading hours. If the market closes earlier than your configured `MarketCloseHour`, the forced exit will trigger on the following trading day.
- Combine the strategy with portfolio- or account-level protections (e.g., `StartProtection`) if you need stop-loss or drawdown limits beyond the original design.
