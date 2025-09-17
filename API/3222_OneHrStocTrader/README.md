# OneHrStocTrader Strategy

## Overview

The **OneHrStocTrader** strategy replicates the MetaTrader 4 expert advisor *OneHrStocTrader.mq4* inside the StockSharp high-level
API. It trades a single instrument on hourly candles and combines the stochastic oscillator with a Bollinger Band width filter. A
trade is opened only when volatility (measured by the distance between the Bollinger Bands) sits inside the configured range and
the stochastic oscillator leaves an extreme zone exactly at the configured hour.

## Trading Logic

1. **Data**
   - Works with hourly candles by default (configurable).
   - Uses the latest *completed* candle values to match the MetaTrader behaviour.
2. **Bollinger Band filter**
   - Calculates the spread between the upper and lower bands in pips.
   - Trading signals are ignored when the spread falls outside the `[BollingerSpreadLower, BollingerSpreadUpper]` range.
3. **Stochastic oscillator trigger**
   - References the two latest completed candles of the stochastic %K line.
   - **Buy**: current %K below `StochasticLower`, previous %K rising (`prev < current`) and the new bar starts at `BuyHourStart`.
   - **Sell**: current %K above `StochasticUpper`, previous %K falling (`prev > current`) and the new bar starts at `SellHourStart`.
4. **Order management**
   - Closes an opposite position before opening a new one.
   - Limits consecutive entries in the same direction via `MaxOrdersPerDirection`.
5. **Risk management**
   - Fixed take-profit and stop-loss distances defined in pips.
   - Optional trailing stop that moves in pip increments once price travels beyond the configured distance.
   - Internal protective levels are monitored on each completed candle; when hit, the strategy closes the position at market.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `TradeVolume` | Order size in lots. | `0.01` |
| `CandleType` | Time frame used for all calculations. | `1h` |
| `BollingerPeriod` | Bollinger Bands look-back period. | `20` |
| `BollingerSigma` | Bollinger Bands sigma multiplier. | `2.0` |
| `BollingerSpreadLower` | Minimum band spread in pips required to trade. | `56` |
| `BollingerSpreadUpper` | Maximum band spread in pips allowed to trade. | `158` |
| `BuyHourStart` | Hour (0-23) when long entries are evaluated. | `4` |
| `SellHourStart` | Hour (0-23) when short entries are evaluated. | `0` |
| `StochasticKPeriod` | Stochastic %K period. | `5` |
| `StochasticDPeriod` | Stochastic %D period. | `3` |
| `StochasticSlowing` | Stochastic slowing factor. | `5` |
| `StochasticLower` | Oversold threshold. | `36` |
| `StochasticUpper` | Overbought threshold. | `70` |
| `TakeProfitPips` | Take-profit distance in pips. | `200` |
| `StopLossPips` | Stop-loss distance in pips. | `95` |
| `TrailingStopPips` | Trailing stop distance in pips (0 = disabled). | `40` |
| `MaxOrdersPerDirection` | Maximum consecutive entries per direction. | `1` |

## Charting

When a chart surface is available, the strategy draws:
- Price candles.
- Bollinger Bands.
- Stochastic oscillator on a separate pane.
- Executed trades for quick visual validation.

## Notes

- Pip size is derived from the instrument price step and decimal precision, mirroring the MetaTrader multiplier logic.
- Protective levels are recalculated using `Security.ShrinkPrice` to ensure exchange-compliant price rounding.
- Trailing-stop adjustments mimic the original EA by tightening the stop only when price advances by at least one pip beyond the
  previous stop.
- The implementation does not create pending orders; all entries and exits use market orders exactly like the source expert
  advisor.
