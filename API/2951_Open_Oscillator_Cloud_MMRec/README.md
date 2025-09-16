# Open Oscillator Cloud MMRec Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the MetaTrader expert advisor **Exp_Open_Oscillator_Cloud_MMRec** to the StockSharp high level API. The system trades the crossover of the Open Oscillator Cloud indicator, which compares the current open price with the opens of the highest and lowest bars inside a rolling window and smooths the result with a configurable moving average.

## Strategy logic

### Indicator construction
- Build a lookback window (`Oscillator Period`, default 20 bars) of finished candles from the selected timeframe.
- Find the bar with the highest high and store its open price, and find the bar with the lowest low and store its open price.
- Compute two raw values for the current candle:
  - **Upper band** = current open − open price at the highest high.
  - **Lower band** = open price at the lowest low − current open.
- Smooth both series with the chosen moving average (`Smoothing Method`, `Smoothing Length`). Supported types are Simple, Exponential, Smoothed, and Weighted moving averages.
- Store the smoothed history and delay the signal by `Signal Bar` fully closed candles (default 1) to mimic the original EA logic that acts on the prior bar.

### Entry conditions
- **Long entry**: previous bar upper band was above the lower band and the latest delayed value crosses below (`upper ≤ lower`). Optionally disabled through `Enable Long Entries`.
- **Short entry**: previous bar upper band was below the lower band and the latest delayed value crosses above (`upper ≥ lower`). Optionally disabled through `Enable Short Entries`.

### Exit conditions
- **Long exit**: previous bar upper band was below the lower band, signalling a bearish regime. Controlled by `Enable Long Exits`.
- **Short exit**: previous bar upper band was above the lower band, signalling a bullish regime. Controlled by `Enable Short Exits`.
- **Risk management**: if `Stop Loss Points` or `Take Profit Points` are greater than zero the strategy automatically closes the position once price reaches those distances (measured in instrument price steps) from the entry.

### Order handling
- Only market orders are used. Before opening a new position the opposite side is flattened to stay aligned with the single-position behaviour of the MetaTrader robot.
- The `Trade Volume` parameter sets the base position size for every entry.

## Parameters
- `Candle Type` – timeframe of the candles used for the oscillator (default 1 hour).
- `Oscillator Period` – number of candles in the rolling window (default 20).
- `Smoothing Method` – moving average applied to the open gaps (Simple, Exponential, Smoothed, Weighted).
- `Smoothing Length` – length of the smoothing moving average (default 10).
- `Signal Bar` – number of fully closed bars to delay the signal evaluation (default 1).
- `Enable Long Entries` / `Enable Short Entries` – allow or block opening trades in each direction.
- `Enable Long Exits` / `Enable Short Exits` – allow or block automatic exits for the respective direction.
- `Trade Volume` – size of every market order (default 1 contract/lot).
- `Stop Loss Points` – protective stop distance in price steps (0 disables the stop, default 1000).
- `Take Profit Points` – profit target distance in price steps (0 disables the target, default 2000).

## Implementation notes
- The smoothing methods match the most common options of the original EA. Exotic modes such as JJMA, T3, VIDYA, or AMA are not ported because StockSharp already provides rich alternatives for optimisation and robustness.
- Signals are evaluated only on `CandleStates.Finished` events to avoid acting on incomplete data.
- The strategy keeps an internal history of smoothed values instead of querying indicator buffers, which aligns with the recommended high level StockSharp workflow.
- Protective levels are cleared automatically when the position becomes flat to prevent stale stops from reopening trades.

## Default behaviour
- Trend following across both directions with delayed confirmation to reduce noise.
- Uses fixed money management (constant `Trade Volume`) while honouring stop loss and take profit distances similar to the MetaTrader version.
- Suitable as a template for experimenting with different smoothing types or combining the oscillator with additional filters.
