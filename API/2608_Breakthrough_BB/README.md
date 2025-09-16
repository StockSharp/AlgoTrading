# Breakthrough BB Strategy

## Overview
The Breakthrough BB Strategy replicates the MetaTrader expert advisor *Breakthrough_BB* within the StockSharp high-level API. The system combines Bollinger Bands with a fast simple moving average to capture explosive breakouts that occur after price compresses near the band boundaries. Trades are generated exclusively on completed candles to keep signals deterministic and to mirror the original MQL5 behaviour.

## Trading Logic
- **Trend filter:** A simple moving average (SMA) with configurable period validates the trend direction. The strategy compares the latest SMA value with the SMA value from four bars earlier. Long trades require the SMA to slope upward, while shorts require a downward slope.
- **Bollinger Bands breakout:** The strategy observes how the close from four bars ago interacted with the Bollinger upper or lower band and compares it with the most recent closing price. A valid breakout occurs when price moves from inside the band to outside the band between those two timestamps.
- **Single position model:** The algorithm keeps at most one open position. Any open trade is closed before evaluating new entries to prevent overlapping exposure.

## Entry Conditions
### Long setup
1. The closing price from four completed candles ago was below the upper Bollinger Band.
2. The most recent closing price finished above the current upper Bollinger Band.
3. The SMA value calculated on the latest candle is greater than the SMA value from four candles ago (positive slope).
4. No position is currently open.

### Short setup
1. The closing price from four completed candles ago was above the lower Bollinger Band.
2. The most recent closing price finished below the current lower Bollinger Band.
3. The SMA value calculated on the latest candle is lower than the SMA value from four candles ago (negative slope).
4. No position is currently open.

When an entry condition is satisfied the strategy sends a market order using the configured volume parameter.

## Exit Rules
- **Long position exit:** If a long trade is active and the latest close falls below the Bollinger middle line, the position is closed immediately with a market sell order.
- **Short position exit:** If a short trade is open and the latest close climbs above the Bollinger middle line, the position is covered with a market buy order.

These exit rules mimic the original expert advisor, which removed trades whenever the market reverted back inside the band midline.

## Indicators
- **Simple Moving Average (SMA):** Defines the directional bias and provides the slope comparison over a four-candle interval.
- **Bollinger Bands:** Supplies the upper, middle, and lower envelopes used to detect breakout entries and manage exits.

## Parameters
| Name | Description | Default | Optimizable |
| --- | --- | --- | --- |
| `MaPeriod` | Length of the SMA used for the trend filter. | `9` | ✔ |
| `BandsPeriod` | Lookback length for Bollinger Band calculations. | `28` | ✔ |
| `Deviation` | Standard deviation multiplier applied to Bollinger Bands. | `1.6` | ✔ |
| `Volume` | Order size (in lots or contracts, depending on the instrument). | `1` | ✔ |
| `CandleType` | Candle aggregation type processed by the strategy. | `1 hour` time frame | ✖ |

All parameters expose StockSharp `StrategyParam` metadata so they can be adjusted in the UI or optimized in the designer.

## Data Requirements
- Works with any instrument that provides candle data compatible with the selected `CandleType`.
- Signals are evaluated only on finished candles. Incomplete candles are ignored to keep the logic deterministic.
- The default configuration uses hourly candles, but any timeframe supported by the data source can be supplied.

## Additional Notes
- The algorithm refrains from using indicator history lookups and instead maintains a rolling four-bar cache for close and SMA values, staying within project guidelines.
- Protective features such as stop-loss or take-profit can be added via `StartProtection` if desired; they are not part of the original MQL implementation and are therefore omitted here.
- Because the strategy issues market orders, ensure sufficient liquidity on the chosen instrument to minimize slippage.
