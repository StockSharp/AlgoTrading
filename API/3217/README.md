# Harami Strategy

## Overview
HaramiStrategy converts the MetaTrader "Harami" expert advisor into StockSharp's high-level API. The strategy combines a bullish/bearish Harami pattern detected on a higher timeframe with momentum expansion and a long-term MACD filter. Only completed candles are processed and all trade management is handled through StockSharp's built-in protection engine.

## Data and indicators
- **Base timeframe:** configurable (15-minute candles by default) for moving-average trend detection.
- **Higher timeframe:** configurable (defaults to one hour) for pattern recognition and momentum confirmation.
- **MACD timeframe:** configurable (defaults to 30-day candles) to emulate the original monthly MACD filter.
- **Indicators:**
  - Linear Weighted Moving Average (`FastMaLength`) on the base timeframe.
  - Exponential Moving Average (`SlowMaLength`) on the base timeframe.
  - Momentum (`MomentumPeriod`) on the higher timeframe. The strategy uses the absolute distance from the neutral value (100) for the latest three higher-timeframe bars.
  - Moving Average Convergence Divergence (12/26/9) on the MACD timeframe.

## Long setup
1. Slow EMA is above the fast LWMA on the base timeframe, signalling an uptrend.
2. The higher timeframe forms a bullish Harami sequence: two candles ago was bearish, the previous candle was bullish, and its body is smaller than the earlier bearish body.
3. Any of the last three higher-timeframe momentum deviations exceed `MomentumBuyThreshold`.
4. MACD main line is above the signal line on the MACD timeframe.
5. No long position is open (`Position <= 0`).
6. The strategy sends a market buy order sized to flip any short exposure and add `Volume` lots.

## Short setup
1. Slow EMA is below the fast LWMA on the base timeframe.
2. The higher timeframe forms a bearish Harami: two candles ago was bullish, the previous candle was bearish, and the latest body is smaller.
3. Any of the last three higher-timeframe momentum deviations exceed `MomentumSellThreshold`.
4. MACD main line is below the signal line.
5. No short exposure is open (`Position >= 0`).
6. The strategy sends a market sell order large enough to close long positions and open a new short position of size `Volume`.

## Risk management
`StartProtection` installs stop-loss and take-profit levels (expressed in points). Additional trailing, break-even and money-management features from the original EA are intentionally omitted to keep the StockSharp version concise. Trade direction changes automatically flatten the opposite exposure.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Primary timeframe for moving averages and signal execution. | 15-minute candles |
| `HigherCandleType` | Timeframe used for Harami and momentum confirmation. | 1-hour candles |
| `MacdCandleType` | Timeframe for the MACD trend filter. | 30-day candles |
| `FastMaLength` | Fast linear weighted MA length. | 6 |
| `SlowMaLength` | Slow exponential MA length. | 85 |
| `MomentumPeriod` | Momentum lookback on the higher timeframe. | 14 |
| `MomentumBuyThreshold` | Minimum momentum deviation for long confirmation. | 0.3 |
| `MomentumSellThreshold` | Minimum momentum deviation for short confirmation. | 0.3 |
| `StopLossPoints` | Stop-loss distance in points. | 40 |
| `TakeProfitPoints` | Take-profit distance in points. | 100 |

## Usage tips
- Align `CandleType`, `HigherCandleType` and `MacdCandleType` with available historical data; ensure the higher timeframe is longer than the base timeframe.
- Adjust momentum thresholds to match the volatility of the traded instrument.
- Use StockSharp's optimizer through the provided parameter ranges to tune MA lengths and momentum thresholds.
- Always backtest with realistic commission/latency settings before deploying live.
