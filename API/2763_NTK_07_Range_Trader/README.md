# NTK 07 Range Trader Strategy

The NTK 07 Range Trader Strategy is a port of the MetaTrader "NTK 07" expert advisor. The algorithm maintains symmetrical stop orders around the current market price and manages open positions with configurable trailing and take-profit logic. The goal is to capture breakouts that occur near the borders or the center of a short-term price range while respecting strict risk controls.

## Core Ideas

- **Entry Triggers** – When the strategy is flat it evaluates a configurable lookback range. If the price sits at the edges of the range or near its midpoint (depending on the selected trade mode) it places both buy stop and sell stop orders at an offset defined in price steps.
- **Range Awareness** – Highest and lowest prices of the last *N* finished candles define the trading range. A zero length disables the filter and allows orders to be placed immediately.
- **Adaptive Risk** – Each entry uses the base volume while an optional lot multiplier can pyramid additional stop orders after a position is opened. A portfolio-wide volume limit blocks new orders when exposure would exceed the cap.
- **Exit Management** – As soon as a position is filled the opposite stop order is cancelled. The strategy then registers protective stop and optional take-profit orders using the configured offsets. Trailing can follow the previous candle’s high/low, a moving average, or a fixed-distance buffer.
- **Session Filter** – Trading is allowed only between the selected start and end hours and is automatically disabled on weekends.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Entry Volume** | Base size for each entry order. |
| **Total Volume Limit** | Maximum cumulative position size. A value of `0` disables the cap. |
| **Net Step** | Distance in price steps between the market and the entry stop orders. |
| **Stop Loss** | Initial stop-loss offset in price steps relative to the entry price. |
| **Take Profit** | Take-profit distance in price steps. Set to `0` to disable profit targets. |
| **Trailing Stop** | Distance in price steps used for trailing logic. |
| **Lot Multiplier** | Multiplier applied when pyramiding into an existing position. |
| **Trail High/Low** | If enabled, protective stops trail the previous candle extremes. |
| **Trail Moving Average** | Enables trailing using a moving average value. Only one trailing mode can be active. |
| **Trading Start/End Hour** | Inclusive platform time window for trading. |
| **Range Bars** | Number of completed candles used to compute the trading range. `0` disables the filter. |
| **Trade Mode** | `EdgesOfRange` requires price to touch the range borders, `CenterOfRange` waits until price is near the range midpoint. |
| **MA Period** | Length of the moving average used for trailing. |
| **Candle Type** | Candle aggregation used for all calculations. |

## Workflow

1. **Data Subscription** – The strategy subscribes to the configured candle series and calculates the moving average as well as the highest and lowest price over the chosen range length.
2. **Flat State** – While no position is open the strategy evaluates the range condition. If satisfied it places paired buy stop and sell stop orders at the specified offset while respecting the global volume limit.
3. **Position Handling** – When an entry fills, the opposing stop is cancelled. The strategy immediately places protective stop-loss and optional take-profit orders. Trailing logic then updates the protective stop on each new finished candle.
4. **Pyramiding** – If the lot multiplier is greater than `1`, an additional stop order is placed in the direction of the current position as long as the total volume limit allows it.
5. **Exit** – Stops or take-profits flatten the position and cancel remaining protective orders. The system then reverts to monitoring for the next range interaction.

## Notes

- The strategy works entirely with price steps, making it suitable for instruments with different tick sizes.
- Trading is automatically disabled on Saturdays and Sundays to mirror the behaviour of the original MQL implementation.
- Only one trailing mode can be enabled at a time; enabling both will trigger a configuration error at startup.
