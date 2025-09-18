# DoubleUp2 Martingale Strategy

## Overview
The DoubleUp2 Martingale strategy reproduces the original MetaTrader expert by combining the Commodity Channel Index (CCI) and the MACD oscillator. Trades are opened only when both indicators reach extreme levels in the same direction. Position sizing follows a martingale scheme where the volume doubles after a losing trade. Profitable trades are partially locked by closing the position once price travels a configurable distance in favor of the position.

## How It Works
1. Subscribe to a single candle series (default 1 minute) and calculate CCI and MACD on every completed bar.
2. Detect extreme momentum:
   * Enter short when both CCI and MACD exceed the positive threshold.
   * Enter long when both drop below the negative threshold.
3. Before reversing, the current position is closed and the martingale step is updated based on the simulated profit of the last trade.
4. Trade volume equals the base volume derived from account equity divided by a balance divisor, multiplied by the martingale factor raised to the current step.
5. Lock in profits by closing any open position once price advances by a predefined number of points from the last entry. Winning exits increase the martingale step by two to match the original EA behaviour.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `CciPeriod` | Lookback period for the CCI indicator. | 8 |
| `MacdFastPeriod` | Fast EMA length for MACD. | 13 |
| `MacdSlowPeriod` | Slow EMA length for MACD. | 33 |
| `MacdSignalPeriod` | Signal EMA length for MACD smoothing. | 2 |
| `Threshold` | Absolute indicator threshold that must be exceeded to trigger entries. | 230 |
| `ExitDistancePoints` | Profit distance in points that triggers position closure. | 120 |
| `BalanceDivisor` | Divisor applied to portfolio equity to obtain base volume. | 50001 |
| `MinimumVolume` | Lower limit for the computed trade volume. | 0.1 |
| `MartingaleMultiplier` | Multiplier applied to position size after each losing close. | 2 |
| `CandleType` | Candle timeframe used for all calculations. | 1 minute |

## Notes
* The martingale logic increases position size after losses and resets after profitable reversals, mirroring the source MQL logic.
* Price step information is used to convert the exit distance (points) into absolute price units. If the instrument does not provide a price step, a value of 1 is used.
* The strategy expects a single instrument and does not place simultaneous long and short positions.
