# Basic ATR Stop Take

## Overview
Basic ATR Stop Take ports the MetaTrader 4 expert advisor **“Basic ATR stop_take expert adviser”** to the StockSharp high-level strategy API. The system is intentionally minimal: it opens exactly one market position in the chosen direction, calculates an Average True Range (ATR) on the working candles, and attaches protective stop-loss and take-profit levels derived from ATR multipliers. Once the trade is closed by either level, the strategy immediately prepares for the next setup in the same direction.

## Strategy logic
### Indicator foundation
* **Average True Range (ATR)** – computed on the subscribed candle type with a configurable lookback. The indicator measures recent volatility and scales both the stop and target distances.

### Entry rules
* Executes on the close of each finished candle after the ATR is fully formed.
* If no position is open and the direction parameter is set to **Buy**, a market buy order is sent using the configured volume.
* If no position is open and the direction parameter is set to **Sell**, a market sell order is sent with the configured volume.
* Choosing **None** disables new entries while keeping existing positions managed until they close.

### Exit rules
* **ATR stop-loss** – distance equals `ATR × Stop Factor`. For longs the stop is placed below the entry; for shorts it is placed above the entry. When the candle’s extreme crosses the level, the position is closed at market.
* **ATR take-profit** – distance equals `ATR × Take Factor`. For longs the profit target sits above the entry; for shorts it sits below. Reaching the level closes the trade at market.
* If either multiplier is set to `0`, the corresponding level is disabled; the strategy continues to monitor the remaining level if present.

### Position management
* Only one position is allowed at a time. After an exit the strategy waits for the next candle close before re-entering in the same direction.
* `StartProtection()` is invoked during start-up so that external manual positions are monitored by the StockSharp protection subsystem.

## Parameters
* **Trade Direction** – side of the market to trade (`None`, `Buy`, or `Sell`).
* **Trade Volume** – order volume for the single market entry.
* **ATR Period** – number of candles used in the ATR calculation.
* **Stop Factor** – ATR multiplier applied to the stop-loss distance. Zero disables the protective stop.
* **Take Factor** – ATR multiplier applied to the take-profit distance. Zero disables the profit target.
* **Candle Type** – timeframe of the candles used for ATR calculation and trade management.

## Additional notes
* The default parameters replicate the EA’s behavior (long-only mode, 0.01 lot volume, ATR period 14, stop factor 1.5, take factor 2.0).
* Price comparisons use candle highs and lows, meaning stop-loss and take-profit triggers occur as soon as the level is pierced within the candle range.
* The strategy does not stack or reverse positions; instead it always flattens and waits for the next bar close before placing a fresh order.
* Only the C# implementation is provided in this package; there is no Python version for this strategy.
