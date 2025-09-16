# IBS RSI CCI v4 Strategy

## Overview
The **IBS RSI CCI v4 Strategy** is a contrarian trading system that combines three momentum oscillators:

- **Internal Bar Strength (IBS)** – measures the relative closing position within the bar's high-low range and is smoothed with a configurable moving average.
- **Relative Strength Index (RSI)** – captures market momentum around the neutral 50 level.
- **Commodity Channel Index (CCI)** – evaluates price deviation from a moving average baseline.

The three components are scaled and blended into a composite oscillator. The composite signal is constrained by a configurable step threshold and filtered through a Donchian-style high/low envelope. Crossovers between the composite signal and its midline generate reversal opportunities.

## Trading Logic
1. Subscribe to candles with the selected timeframe (default: 4 hours).
2. Calculate the IBS value for every finished candle and smooth it with the chosen moving average type.
3. Obtain RSI and CCI values using their respective lookback lengths.
4. Build the composite oscillator using the original weighting from the MetaTrader script:
   - IBS contribution × 700
   - RSI deviation from 50 × 9
   - Raw CCI value × 1
5. Apply a step threshold to avoid sudden jumps in the composite signal.
6. Track the rolling maximum and minimum of the composite signal and smooth both edges to form a dynamic band. The midline of the band is used as the "baseline" (equivalent to the second indicator buffer in the MQL version).
7. **Position management**
   - Close long positions when the composite signal is below the baseline on the confirmed bar.
   - Close short positions when the composite signal is above the baseline on the confirmed bar.
   - Open long positions when the previously confirmed bar was above the baseline and the latest signal crosses down through the baseline (contrarian entry).
   - Open short positions when the previously confirmed bar was below the baseline and the latest signal crosses up through the baseline.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle series used for indicator calculations. |
| `IbsPeriod` | Lookback length used to smooth the IBS component. |
| `IbsAverageType` | Moving average type for IBS smoothing (Simple, Exponential, Smoothed, Linear Weighted). |
| `RsiPeriod` | RSI lookback length. |
| `CciPeriod` | CCI lookback length. |
| `RangePeriod` | Window size for the rolling high/low band applied to the composite signal. |
| `SmoothPeriod` | Moving average length used to smooth the high/low band edges. |
| `RangeAverageType` | Moving average type for the band smoothing (Simple, Exponential, Smoothed, Linear Weighted). |
| `StepThreshold` | Maximum adjustment applied when the composite signal jumps sharply between bars. |
| `SignalBar` | Number of already closed candles used for confirmation (default 1 replicates the original behaviour). |
| `EnableLongOpen` | Allow opening new long positions. |
| `EnableShortOpen` | Allow opening new short positions. |
| `EnableLongClose` | Allow closing existing long positions. |
| `EnableShortClose` | Allow closing existing short positions. |
| `OrderVolume` | Base market order volume submitted on entries. |

## Implementation Notes
- The step constraint replicates the buffer limiting logic from the MQL indicator. A higher `StepThreshold` allows larger jumps in the composite oscillator.
- Only the four most common moving average families are supported for IBS and envelope smoothing, because the StockSharp standard library does not include the custom filters from the MetaTrader resource file.
- The strategy uses `SignalBar` to delay signals by one fully closed candle, matching the original expert advisor behaviour.
- By default the strategy is fully contrarian: signals are generated against the direction of the latest crossover. Toggle the entry/exit booleans to limit the strategy to a single direction if desired.

## Usage
1. Configure the `CandleType` to match your target instrument timeframe.
2. Adjust indicator lengths and the step threshold to fit the instrument's volatility.
3. Enable or disable long/short entries and exits according to your trading preference.
4. Set the `OrderVolume` parameter to control order size and start the strategy. `StartProtection()` is enabled by default and may be customised if additional risk rules are required.
5. Review the chart panel (if available) to monitor candle prices, the composite oscillator, and recorded trades.

## Differences from the MetaTrader Version
- Money management and order deviation parameters from the original EA are replaced with StockSharp's `OrderVolume` parameter and high-level market orders.
- The StockSharp conversion keeps the original indicator weights and reversal logic but focuses on the most commonly used moving average filters.
- Protective stops are not preconfigured; combine the strategy with StockSharp risk modules if fixed stops or take profits are required.
