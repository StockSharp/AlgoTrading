# MACD Sample Hedging Grid Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader "MACD Sample Hedging Grid" expert advisor. It combines a short-term MACD crossover, a local EMA slope filter, and higher timeframe confirmations. When conditions align the strategy builds a grid of positions in the detected direction, scaling the trade size by a configurable exponent.

## Market Logic
- **Base timeframe:** configurable (default 5-minute candles).
- **Trend filter:** an EMA (default 26 periods) must slope upward for long trades or downward for short trades.
- **MACD trigger:** the fast MACD line must cross the signal line on the base timeframe while exceeding a minimum absolute value (expressed in price steps).
- **Momentum confirmation:** the absolute distance between momentum and the neutral 100 level on a higher timeframe must exceed separate thresholds for longs and shorts. The last three higher timeframe candles are inspected, replicating the original EA behaviour.
- **Long-term confirmation:** a MACD calculated on a long timeframe (monthly by default) must agree with the trade direction (MACD above signal for bullish, below for bearish environments).

Once a signal fires, the strategy either starts a new grid in that direction or adds to the existing grid as long as the maximum number of entries has not been reached.

## Position Management
- **Grid sizing:** each additional entry multiplies the initial volume by the `LotExponent` (default 1.44). Position size resets when the direction changes or the position is closed.
- **Risk controls:** optional take-profit and stop-loss distances are translated into StockSharp protective orders in price steps.
- **Direction change:** whenever an opposite signal arrives the current exposure is flattened before opening the grid in the new direction.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Main timeframe used for MACD and EMA calculations. | 5-minute time frame |
| `MomentumCandleType` | Higher timeframe feeding the momentum confirmation. | 30-minute time frame |
| `TrendCandleType` | Long timeframe used for the trend MACD filter. | 30-day time frame |
| `FastMaPeriod` | Fast EMA length inside MACD. | 12 |
| `SlowMaPeriod` | Slow EMA length inside MACD. | 26 |
| `SignalPeriod` | Signal SMA length for MACD. | 9 |
| `TrendMaPeriod` | EMA length for the local trend filter. | 26 |
| `MomentumPeriod` | Momentum indicator length (higher timeframe). | 14 |
| `MacdOpenLevel` | Minimum absolute MACD level (in price steps) required for a trade. | 3 |
| `MomentumBuyThreshold` | Minimum absolute momentum distance from 100 for longs. | 0.3 |
| `MomentumSellThreshold` | Minimum absolute momentum distance from 100 for shorts. | 0.3 |
| `MaxTrades` | Maximum number of grid entries per direction. | 10 |
| `LotExponent` | Multiplier used for each additional grid entry. | 1.44 |
| `StopLossSteps` | Stop-loss distance measured in price steps. | 20 |
| `TakeProfitSteps` | Take-profit distance measured in price steps. | 50 |

## Notes
- The original EA also contained money-based trailing, break-even moves, and account equity stops. These features require broker-specific portfolio data and manual order management; they are not implemented in this high-level StockSharp conversion.
- Candle subscriptions, indicator bindings, and trade execution follow StockSharp's recommended high-level API usage.
- Ensure the selected instruments support the configured candle types and that historical data is available for all referenced timeframes.
