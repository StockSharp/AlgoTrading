# Cronex RSI Strategy

The **Cronex RSI Strategy** recreates the Exp_CronexRSI.mq5 expert advisor on the StockSharp high-level API. The indicator stack
combines a classic Relative Strength Index (RSI) with two sequential moving averages in order to reduce noise. Trading decisions are
based on crossovers between the fast and the slow smoothed RSI curves, with configurable entry/exit permissions that match the
original MQL5 parameters.

## Trading Logic

1. Build the RSI from the selected applied price and lookback period.
2. Smooth the RSI value with a *fast* moving average, then smooth the result with a *slow* moving average.
3. Evaluate crossovers with a configurable confirmation shift:
   - When the fast curve was above the slow curve one bar earlier and falls below it on the confirmed bar, the strategy closes
     short positions and, if enabled, opens a long position.
   - When the fast curve was below the slow curve and crosses above it on the confirmed bar, the strategy closes longs and can
     enter short trades.
4. Volumes are symmetrical in both directions. When a new signal reverses the position the strategy first covers the existing
   exposure and then opens the new side using the configured base volume.

By default the strategy waits for one fully closed candle before acting on a signal, reproducing the `SignalBar = 1` behaviour from
Exp_CronexRSI. Setting the shift to zero processes the crossover immediately on the closing bar.

## Parameters

| Name | Description |
| ---- | ----------- |
| `RsiPeriod` | RSI lookback period. |
| `FastPeriod` | Length of the fast smoothing moving average. |
| `SlowPeriod` | Length of the second smoothing moving average. |
| `SignalShift` | Number of completed bars used for confirmation (0 reacts instantly). |
| `SmoothingMethod` | Moving-average type applied during both smoothing stages (simple, exponential, smoothed, linear weighted, volume weighted). |
| `AppliedPrice` | Price component passed to the RSI (close, open, high, low, median, typical, weighted). |
| `CandleType` | Candle series processed by the strategy. |
| `TradeVolume` | Base order size used for new entries. |
| `EnableLongEntry` / `EnableShortEntry` | Allow opening long/short positions. |
| `EnableLongExit` / `EnableShortExit` | Allow closing positions in response to opposite signals. |

## Implementation Notes

- The smoothing method uses StockSharp moving average classes. The `VolumeWeighted` option also covers MQL5 VIDYA/AMA styles by
  applying a pragmatic volume-weighted substitute.
- Applied price selection matches the Cronex indicator inputs and mirrors the helper used inside the original expert advisor.
- All indicator values are processed through `DecimalIndicatorValue` instances to stay compatible with StockSharp's indicator
  pipeline while avoiding direct value polling.
- The strategy automatically resizes its internal history when the confirmation shift changes, ensuring that the crossover logic
  keeps the exact lookback structure of the MQL5 version.

## Usage

1. Attach the strategy to a portfolio and security in the StockSharp designer or via code.
2. Configure the candle timeframe, smoothing style and trade permissions to match your preferred Cronex RSI setup.
3. Launch the strategy. It will subscribe to the selected candle series, update the RSI/MA combination and send market orders on
   confirmed crossovers.
4. Use the built-in chart helpers to visualise the indicator curves and executed trades for further validation.
