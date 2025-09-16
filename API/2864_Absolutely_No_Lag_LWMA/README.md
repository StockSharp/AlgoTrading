# Absolutely No Lag LWMA Strategy

## Overview
The strategy replicates the MetaTrader expert advisor **Exp_AbsolutelyNoLagLwma** by applying a double weighted moving average (LWMA) to candle data. The indicator output is color-coded: green (2) for an upward slope, gray (1) for flat, and magenta (0) for a downward slope. Trade decisions are based on transitions between these color states. The StockSharp implementation uses the high-level API, subscribes to timeframe candles, and sends market orders according to the detected trend direction.

## Trading Logic
### Indicator pipeline
1. Select the desired price series defined by the *Price Type* parameter.
2. Apply a weighted moving average (LWMA) with the configured *LWMA Length*.
3. Smooth the result with a second LWMA of the same length.
4. Compare the smoothed LWMA value with the previous value to classify the slope direction:
   - **2 (uptrend)** – current value is greater than the previous value.
   - **1 (neutral)** – current value equals the previous value.
   - **0 (downtrend)** – current value is less than the previous value.

### Signal evaluation
- Only completed candles are processed. The *Signal Bar* parameter shifts the signal evaluation to historic candles (1 = previous finished candle, 2 = candle before that, etc.). The strategy also remembers the color of the bar that precedes the selected signal candle to avoid duplicate entries.
- **Bullish transition**: the selected signal candle is color **2** and the previous candle is not **2**. This opens longs (if enabled) and closes existing shorts.
- **Bearish transition**: the selected signal candle is color **0** and the previous candle is not **0**. This opens shorts (if enabled) and closes existing longs.

### Position management
- Orders are executed with market orders. The requested volume is `Volume + |Position|` when flipping the direction so that the opposite position is closed automatically.
- Exit signals can be toggled independently from entries, allowing signal-only or exit-only behaviour.
- `StartProtection()` is activated to engage the common StockSharp protective logic once the strategy starts.

## Parameters
- **LWMA Length** – length of the two LWMAs used for smoothing.
- **Price Type** – price source that feeds the LWMA (close, open, high, low, median, typical, weighted, simplified, quarter, trend-follow variations, or Demark price).
- **Signal Bar** – number of finished candles back used for signal evaluation.
- **Enable Long Entries** – permits opening long positions on bullish transitions.
- **Enable Short Entries** – permits opening short positions on bearish transitions.
- **Enable Long Exits** – closes long positions when the indicator turns bearish.
- **Enable Short Exits** – closes short positions when the indicator turns bullish.
- **Candle Type** – timeframe of the candle subscription used by the indicator.
- **Volume** (built-in Strategy property) – trade size for new entries.

## Notes
- The default timeframe is 4 hours, matching the original expert advisor configuration, but it can be adjusted through the *Candle Type* parameter.
- No take-profit or stop-loss orders are placed automatically; users can combine the strategy with StockSharp risk management components if required.
- The Python port is intentionally omitted as requested.
