# Mean Reverse Strategy

## Overview
The Mean Reverse Strategy replicates the "MeanReversionTrendEA" expert advisor. It mixes a moving average crossover trend module with a mean-reversion overlay driven by Average True Range (ATR) volatility bands. The idea is to open a position when price either confirms a bullish or bearish trend shift or stretches too far away from the slower moving average by a volatility-adjusted distance.

## Trading Logic
- **Trend component**: a long setup appears when the fast simple moving average (SMA) crosses above the slow SMA. A short setup is triggered when the fast SMA crosses below the slow SMA.
- **Mean-reversion component**: a long setup is activated whenever the closing price dips below the slow SMA by more than `ATR × Multiplier`. A short setup appears when price rallies above the slow SMA by more than the same distance.
- **Signal combination**: if either the trend module or the mean-reversion module signals a long (short) while no position is open, the strategy enters a long (short) position with the configured volume.

## Trade Management
- **Stop-loss**: immediately after entry the strategy places a price level at `entry − StopLossPoints × Step` for long positions or `entry + StopLossPoints × Step` for short positions. When candle extremes touch this level the position is closed.
- **Take-profit**: a profit target is placed at `entry + TakeProfitPoints × Step` for long trades or `entry − TakeProfitPoints × Step` for short trades. A touch on the respective candle high or low closes the position.
- **Single position constraint**: the algorithm keeps at most one open position. New signals are ignored until the current trade is closed.
- **Safety module**: the built-in `StartProtection()` call mirrors the safety-trade validation layer from the original expert advisor and guards against unexpected position states.

## Indicators
- **Simple Moving Average (SMA)** with period `FastMaPeriod`.
- **Simple Moving Average (SMA)** with period `SlowMaPeriod`.
- **Average True Range (ATR)** with period `AtrPeriod`.

All indicators are updated from the same candle subscription defined by `CandleType`.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `FastMaPeriod` | Lookback of the fast SMA used in both trend detection and mean-reversion bands. | 20 |
| `SlowMaPeriod` | Lookback of the slow SMA that represents the equilibrium mean. | 50 |
| `AtrPeriod` | Number of candles for ATR volatility calculation. | 14 |
| `AtrMultiplier` | Multiplier applied to ATR for distance checks. | 2.0 |
| `StopLossPoints` | Stop-loss distance measured in `Security.Step` units. | 500 |
| `TakeProfitPoints` | Take-profit distance measured in `Security.Step` units. | 1000 |
| `TradeVolume` | Volume sent with each market order. | 1 |
| `CandleType` | Candle data type that feeds the indicators. | 1-hour time frame |

## Notes
- The default candle size is one hour to reflect the "current timeframe" logic of the MetaTrader version. Adjust it to match the original chart period.
- ATR-based envelopes use the candle close as the reference price, mirroring the original midpoint between bid and ask.
- Use the optimization flags attached to the parameters to calibrate the system for different markets.
