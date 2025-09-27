# RSI Expert Trend Filter Strategy

## Overview
- Conversion of the MetaTrader 5 expert advisor **RSI_Expert_v2.0** to StockSharp's high level strategy API.
- Generates signals on the configured `CandleType` (default 1 hour) and executes trades at candle close.
- Designed for net positions: the strategy keeps a single aggregate position instead of hedging multiple tickets.

## Entry Logic
1. **RSI crossover** – a long setup appears when the latest RSI value rises above `RsiLevelDown` while the previous finished candle was below the level. A short setup is triggered when RSI falls back under `RsiLevelUp` after being above it.
2. **Moving average filter** – the original expert allows trading with or against a moving average crossover. The `MaMode` parameter reproduces the choices:
   - `Off`: ignore moving averages and trade only on RSI triggers.
   - `Forward`: allow longs only when the fast MA is above the slow MA, shorts only when it is below.
   - `Reverse`: invert the filter so that longs require the fast MA below the slow MA, matching the EA's “reverse” mode.

Both conditions must agree before the strategy opens a new market order. If a position is already open or an order is waiting, new signals are ignored until it finishes.

## Trade Management
- Initial stop loss and take profit are expressed in pips using the instrument `PriceStep`. Both are optional; setting a value of zero disables the respective exit.
- When `TrailingStopPips` is greater than zero the stop will trail the price once profit exceeds `TrailingStopPips + TrailingStepPips`. The step value must be strictly positive when trailing is enabled (the strategy throws otherwise).
- If `UseMartingale` is enabled, the next order volume doubles after the previous position closed with a loss (detected via realized PnL). Winning trades reset the multiplier.

## Money Management
- `MoneyMode = FixedVolume` keeps the same `VolumeOrRiskValue` for every entry.
- `MoneyMode = RiskPercent` treats `VolumeOrRiskValue` as a percentage of portfolio equity and derives the quantity from the configured stop-loss distance. When no stop loss is specified the strategy falls back to the raw value.
- Volumes are normalised to exchange rules using `Security.MinVolume` and `Security.VolumeStep` to avoid invalid order sizes.

## Additional Implementation Notes
- Trailing logic and stop/target checks are evaluated on finished candles to replicate the “new bar” behaviour of the MQL version.
- The martingale flag uses realized PnL changes when a position is closed externally, so manual closures are also tracked.
- Because StockSharp uses aggregate positions, simultaneous long and short trades (MT5 hedging mode) are not supported.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Timeframe used for indicator updates and signal generation. |
| `StopLossPips` | Initial stop-loss distance in pips; zero disables the stop. |
| `TakeProfitPips` | Initial take-profit distance in pips; zero disables the target. |
| `TrailingStopPips` | Trailing stop distance. Requires a positive `TrailingStepPips`. |
| `TrailingStepPips` | Extra pips needed before the trailing stop moves again. |
| `MoneyMode` | Selects fixed lot sizing or risk-percent calculation. |
| `VolumeOrRiskValue` | Lot size in fixed mode or percent risk in risk mode. |
| `UseMartingale` | Doubles the next order volume after a losing trade. |
| `FastMaPeriod` | Period of the fast moving average used by the trend filter. |
| `SlowMaPeriod` | Period of the slow moving average used by the trend filter. |
| `RsiPeriod` | Averaging length for the RSI indicator. |
| `RsiLevelUp` | Upper RSI threshold that triggers short setups. |
| `RsiLevelDown` | Lower RSI threshold that triggers long setups. |
| `MaMode` | Enables or inverts the moving average confirmation filter. |
