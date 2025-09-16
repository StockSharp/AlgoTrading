# Dealers Trade ZeroLag MACD Strategy

## Overview
This strategy ports the MetaTrader expert advisor "Dealers Trade v 7.91 ZeroLag MACD" to the StockSharp high level API. It tracks the slope of a zero lag MACD to decide whether the market is in an accumulation phase for longs or shorts and builds a grid of positions with adaptive spacing and risk management. The default timeframe is four-hour candles as recommended by the original author, but any candle type supported by StockSharp can be selected.

## Trading logic
- **Signal detection.** Two zero lag exponential moving averages (fast and slow) generate a MACD line. When the MACD rises compared to the previous bar the strategy treats the market as bullish; when it falls it treats the market as bearish. The signal can be inverted via the `ReverseCondition` parameter.
- **Position grid.** The algorithm scales into the detected direction. Distances between entries are measured in pips and multiplied after each fill by `IntervalCoefficient`. The lot size is multiplied by `LotMultiplier` on every additional entry, mimicking the martingale scheme from the MQL version.
- **Volume control.** If `BaseVolume` is greater than zero it is used as the initial order quantity. Otherwise the engine derives the size from `RiskPercent`, stop distance and the instrument step parameters. Each calculated volume is checked against the instrument limits and capped by `MaxVolume`.
- **Order management.** Every entry can be equipped with a stop loss, take profit and trailing stop (all in pips). The take profit distance is multiplied by `TakeProfitCoefficient` for successive entries to widen targets.
- **Account protection.** When the total number of open positions exceeds `PositionsForProtection` and their combined profit reaches `SecureProfit`, the strategy closes the trade with the largest profit to lock in gains. If the total number of positions exceeds `MaxPositions` it closes the worst performing trade before accepting new entries.

## Position handling
- Stops, trailing logic and targets are evaluated on finished candles using close, high and low prices.
- All open positions are tracked with their own volume, entry price and trailing state. The last fill price is reused to enforce the minimum spacing for future entries.
- When the account balance falls below `MinimumBalance` the strategy stops itself to avoid overtrading on small accounts.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `BaseVolume` | Initial order size. Set to zero to enable risk-based sizing via `RiskPercent`. |
| `RiskPercent` | Percentage of portfolio equity to risk when position size is derived from the stop distance. |
| `MaxPositions` | Maximum number of simultaneously open entries. |
| `IntervalPips` | Initial spacing between grid entries in pips. |
| `IntervalCoefficient` | Multiplier applied to the spacing after each additional entry. |
| `StopLossPips` | Stop loss distance in pips. Set to zero to disable. |
| `TakeProfitPips` | Base take profit distance in pips. Multiplied by `TakeProfitCoefficient` per entry. |
| `TrailingStopPips` / `TrailingStepPips` | Trailing stop distance and required advance before the trail is adjusted. |
| `TakeProfitCoefficient` | Multiplier for widening take profit distances on later entries. |
| `SecureProfit` | Profit threshold that triggers account protection once enough positions are open. |
| `AccountProtection` | Enables automatic profit locking by closing the best trade. |
| `PositionsForProtection` | Minimum number of open positions required before account protection becomes active. |
| `ReverseCondition` | Inverts the MACD slope interpretation. |
| `FastLength`, `SlowLength`, `SignalLength` | Periods of the zero lag exponential moving averages. |
| `MaxVolume` | Cap for the volume of a single entry. |
| `LotMultiplier` | Multiplicative factor for scaling position size with each grid entry. |
| `MinimumBalance` | Minimal account balance required to continue trading. |
| `CandleType` | Candle data type used for calculations. |

## Usage notes
1. Connect the strategy to a portfolio and security before starting it.
2. Review the instrument step and price settings to ensure pip conversions are correct.
3. The default parameters replicate the original expert advisor behaviour but can be optimised through StockSharp optimisers.
4. Python translation is not included for this strategy.
