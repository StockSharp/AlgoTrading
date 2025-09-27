# Bollinger Band Squeeze Breakout Strategy

## Overview
The strategy replicates the original MetaTrader 4 expert advisor "BOLINGER BAND SQUEEZE" using the StockSharp high level API. It looks for periods where Bollinger Bands contract and then enter trades once the bands expand, provided that momentum and trend filters confirm the move. The conversion keeps the multi-timeframe confirmation logic and transforms the money-management blocks into StockSharp idioms.

## Trading Logic
1. **Band squeeze and expansion**
   - Bollinger Bands (length 20, deviation 2 by default) are calculated on the working timeframe.
   - The width of the most recent completed candle is compared against the width `RetraceCandles` bars ago.
   - A valid breakout requires the width ratio to exceed `SqueezeRatio`, signalling that price is expanding out of the squeeze.
2. **Trend filter**
   - Two weighted moving averages (WMA 6 and WMA 85 on typical price) define the immediate trend. Long trades require the fast WMA to be above the slow WMA, shorts the opposite.
3. **Momentum confirmation**
   - A higher timeframe Momentum indicator (length 14) checks whether price deviates sufficiently from the 100 level. The maximum deviation of the last three higher timeframe values must exceed the direction-specific threshold.
   - The higher timeframe is automatically selected to match the mapping used in the MT4 script (e.g., M15 → H1, H1 → D1, D1 → monthly). Weekly data also falls back to monthly confirmation. If no higher timeframe is available the momentum filter is skipped.
4. **Macro filter**
   - A monthly Moving Average Convergence Divergence (MACD 12/26/9) ensures that longer-term momentum matches the trade direction (MACD line above signal for longs, below for shorts).
5. **Entry rules**
   - Longs: band expansion, fast WMA above slow WMA, monthly MACD bullish, higher timeframe momentum deviation above `MomentumBuyThreshold`, and structural candle overlap (`candle[-2].Low < candle[-1].High`).
   - Shorts: band expansion, fast WMA below slow WMA, monthly MACD bearish, higher timeframe momentum deviation above `MomentumSellThreshold`, and the mirrored candle condition (`candle[-1].Low < candle[-2].High`).
6. **Exit rules**
   - Positions are closed when price closes on or beyond the outer Bollinger Band in the trade direction (i.e., long exits at the upper band, short exits at the lower band), mirroring the MT4 implementation.
   - `StartProtection()` enables StockSharp's protective order infrastructure so stop-loss / take-profit extensions can be added if required.

## Indicators and Data Subscriptions
- Primary timeframe candles defined by `CandleType`.
- Higher timeframe candles for momentum confirmation (auto-mapped from the base timeframe).
- Monthly candles for MACD filtering (30-day approximation).
- Indicators: Bollinger Bands, two Weighted Moving Averages (typical price), Momentum, and MovingAverageConvergenceDivergenceSignal.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 15-minute candles | Primary working timeframe. |
| `BollingerPeriod` | 20 | Bollinger Band length. |
| `BollingerWidth` | 2.0 | Bollinger Band standard deviation multiplier. |
| `SqueezeRatio` | 1.1 | Minimum width expansion ratio between current and historical bands. |
| `RetraceCandles` | 10 | Lookback used for squeeze comparison. |
| `FastMaLength` | 6 | Length of the fast WMA (typical price). |
| `SlowMaLength` | 85 | Length of the slow WMA (typical price). |
| `MomentumLength` | 14 | Momentum period on the higher timeframe. |
| `MomentumBuyThreshold` | 0.3 | Minimum deviation from 100 required to validate long entries. |
| `MomentumSellThreshold` | 0.3 | Minimum deviation from 100 required to validate short entries. |

All parameters are exposed as `StrategyParam<T>` values and can be optimised inside StockSharp Designer or at runtime.

## Implementation Notes
- The strategy uses `SubscribeCandles().BindEx(...)` to keep indicator wiring declarative and avoids manual indicator collections, as required by the high-level API guidelines.
- Weighted moving averages are driven by typical price inside the candle-processing callback to preserve the behaviour of the LWMA calculations in the MT4 script.
- Higher timeframe momentum values are stored in a three-element queue to mimic `iMomentum` lookbacks 1–3 from the original code.
- Monthly MACD values persist in class fields so that every primary-timeframe candle has access to the latest long-term bias.
- Exits triggered by the outer bands replace the MT4 trailing stop/breakeven blocks while retaining the visual intent of closing when price tags the opposite envelope.
- The strategy leaves order sizing to the base `Strategy.Volume`. Position flips automatically offset any existing exposure by adding `Math.Abs(Position)` to the order volume.
