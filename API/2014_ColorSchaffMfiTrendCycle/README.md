# Color Schaff MFI Trend Cycle Strategy

This strategy is a translation of the MQL5 expert `Exp_ColorSchaffMFITrendCycle`.
It employs the **Color Schaff MFI Trend Cycle** indicator, which combines
Money Flow Index values with a double stochastic calculation. The indicator
produces eight color states representing momentum and overbought/oversold zones.

Trading logic:

- When the previous indicator color is **green** (indexes 6-7) and the current
  color drops below the strong uptrend zone, the strategy closes short positions
  and opens a new long position.
- When the previous indicator color is **orange** (indexes 0-1) and the current
  color rises above the strong downtrend zone, the strategy closes long positions
  and opens a new short position.

Parameters:

- `FastMfiPeriod` – period of the fast MFI.
- `SlowMfiPeriod` – period of the slow MFI.
- `CycleLength` – length of the cyclical buffer used in the indicator.
- `HighLevel` / `LowLevel` – overbought and oversold thresholds for the STC
  value.
- `CandleType` – timeframe of the input candles (default 1 hour).

The strategy uses StockSharp high level API and processes only finished candles.
