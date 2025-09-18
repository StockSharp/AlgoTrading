# Broadening Top Strategy

## Overview
Broadening Top Strategy is a trend-following system inspired by the original MetaTrader "Broadening top" expert advisor. The strategy focuses on catching breakouts that appear after a broadening formation by combining trend direction and momentum confirmation. Two linear weighted moving averages, a momentum oscillator and a MACD filter work together to detect bullish and bearish breakouts.

## Trading Logic
1. **Trend filter** – the strategy compares a fast and a slow linear weighted moving average (LWMA). Long trades require the fast LWMA to be above the slow LWMA, while short trades expect the opposite.
2. **Momentum confirmation** – the momentum oscillator is observed on the last three completed candles. A trade is allowed only if any of these values deviates from the neutral level (100) by at least the configured threshold (separate values for longs and shorts).
3. **MACD alignment** – an additional filter checks the MACD line against its signal line. Long positions are triggered only when the MACD line is above the signal line, shorts when it is below.
4. **Position handling** – before opening a trade in the opposite direction the strategy closes the current position, ensuring that only one position is active at a time.

## Risk Management
The strategy uses `StartProtection` to manage protective orders:
- Optional stop-loss and take-profit distances defined in price steps (pips).
- An optional trailing stop with a configurable trailing step.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Order size in lots/contracts. | 1 |
| `FastMaLength` | Length of the fast linear weighted moving average. | 6 |
| `SlowMaLength` | Length of the slow linear weighted moving average. | 85 |
| `MomentumPeriod` | Lookback period for the momentum oscillator. | 14 |
| `MomentumBuyThreshold` | Minimum distance from the neutral momentum level (100) required to allow long entries. | 0.3 |
| `MomentumSellThreshold` | Minimum distance from the neutral momentum level (100) required to allow short entries. | 0.3 |
| `MacdFast` | Fast EMA length inside MACD. | 12 |
| `MacdSlow` | Slow EMA length inside MACD. | 26 |
| `MacdSignal` | Signal EMA length inside MACD. | 9 |
| `TakeProfitPips` | Take-profit distance measured in price steps. | 50 |
| `StopLossPips` | Stop-loss distance measured in price steps. | 20 |
| `TrailingStopPips` | Trailing-stop distance measured in price steps. | 40 |
| `TrailingStepPips` | Additional distance before the trailing stop is updated. | 10 |
| `CandleType` | Candle type/time frame used for calculations. | 15-minute time frame |
| `EnableLongs` | Enable or disable long trades. | true |
| `EnableShorts` | Enable or disable short trades. | true |

## Indicators
- **LinearWeightedMovingAverage** – fast and slow trend filters.
- **Momentum** – confirms market acceleration away from the neutral level.
- **MovingAverageConvergenceDivergenceSignal** – provides directional confirmation via MACD and signal lines.

## Usage Notes
- Momentum thresholds are evaluated on the three most recent completed candles to emulate the original MQL behavior.
- Protective orders (stop-loss, take-profit, trailing stop) are optional and can be disabled by setting the corresponding distance to zero.
- The strategy must be attached to securities that provide price step and decimal information to calculate pip size correctly.
