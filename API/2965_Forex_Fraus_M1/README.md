# Forex Fraus M1 Strategy

## Overview
The Forex Fraus M1 strategy replicates the MetaTrader 5 expert advisor "Forex Fraus M1" in the StockSharp framework. It is a contrarian system that monitors a long-lookback Williams %R oscillator (period 360) on one-minute candles. Whenever the oscillator touches extreme values, the strategy attempts to fade the move, aiming for a quick reversion toward the recent range mid-point. The implementation keeps the original expert's money management, including optional trading hours, static stop-loss and take-profit levels measured in pips, and a pip-based trailing stop.

## Trading Logic
- **Indicator**: Williams %R with a 360-period lookback.
- **Buy signal**: When Williams %R drops below `-99.9`, the market is considered extremely oversold. The strategy sends a market buy order if there is no existing long position. If `CloseOppositePositions` is enabled, any short exposure is closed in the same order request.
- **Sell signal**: When Williams %R rises above `-0.1`, the market is extremely overbought. The strategy issues a market sell order, optionally closing any open long exposure first.
- **Time filter**: When `UseTimeControl` is enabled the strategy only evaluates signals between `StartHour` (inclusive) and `EndHour` (exclusive). If the session wraps midnight (`StartHour > EndHour`), trading is allowed from `StartHour` to 23 and from 0 to `EndHour - 1`.

## Risk Management
- **Stop-loss**: Calculated as `StopLossPips * PipSize` below (for longs) or above (for shorts) the entry price. When the candle low touches the stop level, the position is closed at market.
- **Take-profit**: Calculated as `TakeProfitPips * PipSize` above (for longs) or below (for shorts) the entry price. When the candle high/low reaches this level, the position is closed to secure profits.
- **Trailing stop**: If both `TrailingStopPips` and `TrailingStepPips` are positive, the stop is tightened once price moves by at least `TrailingStopPips + TrailingStepPips` pips in favor of the trade. For longs the stop trails the close minus `TrailingStopPips`; for shorts it trails the close plus `TrailingStopPips`.
- **Pip size**: `PipSize` defines the monetary value of one pip. For five-digit Forex symbols set `PipSize` to `0.0001`, for three-digit JPY pairs use `0.01`, etc.

The strategy checks stop-loss and take-profit conditions using candle highs/lows. When both are touched within the same candle, the protective stop takes precedence, mirroring the conservative behavior of the original expert.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `OrderVolume` | `0.1` | Trade volume used for new positions. |
| `StopLossPips` | `50` | Stop-loss distance in pips from the entry price. Set to zero to disable. |
| `TakeProfitPips` | `150` | Take-profit distance in pips from the entry price. Set to zero to disable. |
| `TrailingStopPips` | `1` | Base trailing stop distance in pips. Set to zero to disable trailing. |
| `TrailingStepPips` | `1` | Minimum additional pip gain before the trailing stop moves. |
| `UseTimeControl` | `true` | Enables the intraday session filter. |
| `StartHour` | `7` | Start hour for the trading session (0-23). |
| `EndHour` | `17` | End hour for the trading session (1-24, exclusive). |
| `CloseOppositePositions` | `true` | If enabled, reverses existing positions in a single order. |
| `WilliamsPeriod` | `360` | Lookback period for the Williams %R indicator. |
| `CandleType` | `1 minute` | Candle type used to evaluate Williams %R and trading rules. |
| `PipSize` | `0.0001` | Value of a single pip in price units. |

## Additional Notes
- The strategy uses StockSharp's high-level candle subscription API and indicator binding for concise logic without manual buffer management.
- Stop-loss, take-profit, and trailing computations happen on completed candles to avoid acting on unfinished price data.
- The implementation calls `StartProtection()` once on startup to align with the project guidelines, while actual risk handling is managed inside the strategy logic.
- Adjust the `PipSize` parameter to match the traded instrument so that pip-based distances map correctly to price movements.
