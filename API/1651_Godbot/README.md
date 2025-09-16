# Godbot Strategy

This strategy trades using Bollinger Bands combined with moving averages to detect reversals and trend strength.

## Logic
- Works on a main candle timeframe (default 30 minutes).
- Calculates Bollinger Bands and an EMA on this timeframe.
- Separately calculates a DEMA on a higher timeframe (default 1 day) to determine the global trend.
- Closes long positions when price falls back below the upper Bollinger band.
- Closes short positions when price rises back above the lower Bollinger band.
- Opens long when price crosses above the lower band while both DEMA and EMA are rising.
- Opens short when price crosses below the upper band while both DEMA and EMA are falling.

## Parameters
- **Bollinger Period** – period of the Bollinger Bands.
- **Bollinger Deviation** – width multiplier for the bands.
- **EMA Period** – period for the EMA trend filter.
- **DEMA Period** – period for the higher timeframe DEMA.
- **Candle Type** – timeframe used for Bollinger Bands and EMA calculations.
- **DEMA Candle Type** – higher timeframe used for DEMA.

## Notes
- Only one position is held at a time.
- The strategy uses market orders for entries and exits.
- DEMA data must accumulate before trading begins.
