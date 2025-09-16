# Nova Strategy

## Overview
- Conversion of the MetaTrader 5 "Nova" expert advisor that monitors price momentum over a fixed number of seconds.
- Works with any candle type chosen through the `CandleType` parameter and evaluates logic only on finished candles.
- Tracks the best ask and bid prices using Level1 data and stores their values from `SecondsAgo` seconds earlier.
- Enters a **long** position when the previous candle is bullish and the current ask is higher than the stored ask by at least `StepPips`.
- Enters a **short** position when the previous candle is bearish and the current bid is lower than the stored ask by at least `StepPips`.
- Applies automatic stop-loss and take-profit levels using StockSharp protection if the corresponding parameters are greater than zero.
- After a loss (stop-loss activation) the next trade volume is multiplied by `LossCoefficient`; after a profitable exit the volume is reset to `BaseVolume`.

## Parameters
- `SecondsAgo` – number of seconds between the reference price snapshot and the current evaluation moment.
- `StepPips` – breakout filter in pips; converted into price units using the security price step (3/5 decimal instruments are adjusted by ×10).
- `BaseVolume` – initial trade size; normalized to the exchange volume step and min/max limits.
- `StopLossPips` – distance in pips for the protective stop-loss (0 disables it).
- `TakeProfitPips` – distance in pips for the protective take-profit (0 disables it).
- `LossCoefficient` – multiplier applied to the last executed volume after a losing trade.
- `CandleType` – candle source used for signals (timeframe, tick, range, etc.).

## Additional Notes
- The strategy requires Level1 data (best bid/ask) to replicate the original MT5 behaviour; candles provide a fallback using their close price when Level1 is unavailable.
- Volume recalculation respects `Security.VolumeStep`, `Security.MinVolume`, and `Security.MaxVolume` to avoid invalid orders.
- Price conversions rely on `Security.PriceStep` and `Security.Decimals` so the strategy adapts to both 4/5-digit forex symbols and other instruments.
- No Python version is provided for this strategy.
