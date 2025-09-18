# TradingLab Best MACD Strategy

This strategy replicates the MetaTrader expert advisor "TradingLab_Best_MACD_Strategy" using StockSharp's high-level API. It combines moving average structure, MACD crossovers, and dynamic support/resistance checks to open directional trades that align with momentum and recent price reactions.

## Core Logic

- **Candle Source** – Uses the configurable `CandleType` parameter to subscribe to finished candles. Only completed candles generate trading decisions.
- **Trend Filter** – A 200-period simple moving average defines the prevailing trend. Long trades require the close to remain above the average, while short trades require the close to remain below it.
- **Support and Resistance Box** – A 20-period highest/lowest window emulates the "Box" custom indicator. Touching the previous resistance or support level arms short or long setups for a limited number of candles controlled by `SignalValidity`.
- **MACD Crossovers** – A standard MACD (12, 26, 9 by default) must cross its signal line on the previous candle and stay on the required side of the zero line. Each valid crossover keeps its signal alive for `SignalValidity` candles, mirroring the countdown logic from the source EA.
- **Entry Timing** – A position opens when both the MACD and the corresponding support/resistance touch are still valid, and at least one of them triggered on the current candle.
- **Exit Logic** – Upon entry, dynamic stop-loss and take-profit targets are calculated relative to the moving average distance. The take-profit distance is `RiskRewardMultiplier` times the adjusted distance used for the stop. Protective exits monitor subsequent candles and call `ClosePosition()` once price breaches the stored levels.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Fixed volume sent with every market order. |
| `SignalValidity` | Number of candles that keep MACD and support/resistance triggers active. |
| `MaLength` | Period of the simple moving average trend filter. |
| `BoxPeriod` | Lookback length for the highest/lowest box that tracks recent resistance and support. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD fast, slow, and signal periods. |
| `StopDistancePoints` | Distance from the moving average to the stop-loss, expressed in MetaTrader-style points (multiplied by the symbol price step). |
| `RiskRewardMultiplier` | Multiplier applied to the adjusted MA distance to produce the take-profit target. |
| `CandleType` | Data type describing the candle series to subscribe to (default: 1 hour time frame). |

## Notes

- The support and resistance detection follows the original idea by watching whether the previous candle breaks the 20-period highest/lowest levels. Each touch restarts the validity counters.
- Stops and targets are recomputed for every new entry and are checked against the high/low of each finished candle to mimic MetaTrader's intrabar monitoring in a deterministic way.
- Protective management relies on the instrument `PriceStep`. If an instrument reports a zero step, a fallback of 0.0001 is used.
