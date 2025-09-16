# Alli Heik Strategy

The Alli Heik strategy is a conversion of the MetaTrader 5 expert advisor "AlliHeik". It trades the **Heiken Ashi Smoothed Oscillator** (HASO) originally published by mladen. The indicator builds a custom Heiken Ashi candle by smoothing the raw open, high, low, and close prices with a selectable moving average, applies an additional smoothing pass to the Heiken Ashi midpoint, and then measures the bar-to-bar difference of that smoothed value. A moving average of the difference forms the signal line.

Trading decisions are made on the crossover of the oscillator and signal line evaluated on fully closed candles. The strategy offers an optional reverse mode, the ability to automatically close opposite positions, static stop-loss/take-profit handling, and a trailing stop that mimics the step logic of the original MetaTrader version.

## Trading rules

1. **Indicator preparation**
   - Pre-smooth OHLC data with one of SMA, EMA, SMMA, or LWMA.
   - Build Heiken Ashi candles from the smoothed data and average open/close to obtain a midpoint.
   - Post-smooth the midpoint and compute the oscillator as the difference between consecutive smoothed values.
   - Smooth the oscillator with a configurable moving average to create the signal line.
2. **Entry conditions**
   - *Normal mode*: open a **long** when the oscillator crosses **below** the signal line, open a **short** when it crosses **above** the signal line (exactly reproducing the MQL logic).
   - *Reverse mode*: swap the long and short conditions.
   - Signals are evaluated on finished candles only. Existing positions can optionally be closed before entering a new trade in the opposite direction.
3. **Exit management**
   - Static stop-loss and take-profit distances are expressed in pips and converted to price using the security tick size and decimals.
   - A trailing stop becomes active once price advances by *TrailingStop + TrailingStep* pips in profit. The stop is then shifted to `current price - TrailingStop` for longs (or `current price + TrailingStop` for shorts) and only moves if the new stop is at least `TrailingStep` pips beyond the previous level.
   - Manual exits are issued if price touches the configured stop or target.

## Parameters

- **Volume** – order volume in lots.
- **Stop Loss (pips)** – distance for the protective stop; set to 0 to disable.
- **Take Profit (pips)** – distance for the profit target; set to 0 to disable.
- **Trailing Stop (pips)** – trailing stop distance; set to 0 to disable trailing.
- **Trailing Step (pips)** – minimum advance beyond the trailing stop before the stop is moved (must be positive when trailing is enabled).
- **Reverse Signals** – invert long/short interpretation of the oscillator crossover.
- **Close Opposite** – close an existing position before opening a new trade in the opposite direction.
- **Pre Smooth Period / Method** – moving average period and type used to smooth the raw OHLC data.
- **Post Smooth Period / Method** – moving average parameters for smoothing the Heiken Ashi midpoint.
- **Signal Period / Method** – moving average parameters for the oscillator signal line.
- **Candle Type** – candle source used for calculations (default 15-minute time frame).

## Implementation notes

- The conversion reproduces the original Heiken Ashi Smoothed Oscillator by chaining StockSharp moving average indicators (SMA, EMA, SMMA, LWMA) to pre-smooth prices, build the Heiken Ashi series, and derive the oscillator difference.
- Pip distances are translated to absolute price offsets using the security tick size and decimal precision, matching the 3/5 digit handling from MetaTrader.
- Manual stop/target checks and the step-based trailing stop are executed on every finished candle, closely mirroring the behavior of the MQL version.
- Signals are processed only when all required values are available; partial indicator states are ignored until enough data has accumulated.

No Python translation is provided in this directory.
