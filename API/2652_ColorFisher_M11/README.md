# Color Fisher M11 Strategy

## Overview
Color Fisher M11 is a trend-following strategy that replicates the Exp_ColorFisher_m11 expert advisor from MetaTrader 5. It uses a custom Fisher Transform variant that paints candles with five color states to highlight extreme bullish and bearish momentum. Signals are delayed by a configurable number of closed candles to avoid trading on incomplete data, while optional toggles allow disabling entries or exits for each side independently.

## Indicator logic
The strategy builds the Color Fisher indicator in real time:

- Determines the highest high and lowest low over the **Range Periods** window.
- Normalizes the mid-price of the current candle inside that range and applies **Price Smoothing** (EMA-style) to stabilize swings.
- Applies the Fisher Transform with an additional **Index Smoothing** factor to create the final oscillator value.
- Classifies the oscillator into five discrete color bands using the **High Level** and **Low Level** thresholds:
  - `0` – strong bullish impulse above the high level.
  - `1` – moderate bullish momentum between zero and the high level.
  - `2` – neutral zone around zero.
  - `3` – moderate bearish momentum between zero and the low level.
  - `4` – strong bearish impulse below the low level.

The signal is evaluated `Signal Bar` candles back, mimicking the original Expert Advisor behaviour. The previous color state is also tracked to detect fresh transitions into the extreme bands.

## Trading rules
- **Long entry** – allowed when `Enable Buy Entry` is true, the delayed color equals `0` (strong bullish) and the previous color is different from `0`. Any short exposure is reversed and the position turns long.
- **Short entry** – allowed when `Enable Sell Entry` is true, the delayed color equals `4` (strong bearish) and the previous color is different from `4`. Any long exposure is reversed and the position turns short.
- **Long exit** – triggered when `Enable Buy Exit` is true and the delayed color moves to `3` or `4`, signalling bearish control.
- **Short exit** – triggered when `Enable Sell Exit` is true and the delayed color moves to `0` or `1`, signalling bullish control.

To prevent multiple orders per signal, the strategy remembers the next bar close time for each direction and refuses new entries until the next candle is completed.

## Risk management
`Stop Loss (pts)` and `Take Profit (pts)` convert the original pip distances into absolute price steps using the instrument step price. When a positive distance is supplied, protective orders are activated through `StartProtection`. Set either value to zero to disable that protection leg.

## Parameters
- **Range Periods** – lookback length for the high/low range used by the Fisher Transform (default 10).
- **Price Smoothing** – pre-transform smoothing factor, 0…0.99 (default 0.3).
- **Index Smoothing** – post-transform smoothing factor, 0…0.99 (default 0.3).
- **High Level / Low Level** – thresholds that define bullish and bearish extremes (defaults +1.01 and –1.01).
- **Signal Bar** – number of closed candles to delay signal evaluation (default 1).
- **Enable Buy Entry / Enable Sell Entry** – toggles for opening new long or short trades.
- **Enable Buy Exit / Enable Sell Exit** – toggles for allowing indicator-driven exits.
- **Stop Loss (pts) / Take Profit (pts)** – protective distances expressed in price steps.
- **Candle Type** – timeframe for the candle subscription; defaults to 4-hour candles.

## Notes
- The strategy uses high-level StockSharp bindings (`SubscribeCandles().BindEx`) and does not store historical collections beyond the minimal color history required for the delayed signal.
- No Python port is provided in this release, matching the request specification.
- Add the strategy to a chart area to visualize both price and the computed Color Fisher oscillator.
