# RSI Expert Breakout Strategy

## Overview
- Port of the MetaTrader 5 "RSI_Expert" strategy that trades RSI threshold breakouts.
- Uses a single RSI indicator to detect momentum reversals near oversold/overbought regions.
- Implements the original fixed take-profit, stop-loss, and trailing-stop management expressed in pips.

## Strategy Logic
1. Build RSI on the selected candle series (default period: 14).
2. Track the two most recent completed RSI values.
3. Go long when RSI rises back above the lower threshold (20 by default) after previously being below it.
4. Go short when RSI falls back below the upper threshold (60 by default) after previously being above it.
5. Close any opposite exposure before opening a new position to stay net-flat-to-direction.
6. Manage open trades with optional stop loss, take profit, and trailing stop distances measured in pips.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Time frame used for candle aggregation. | 1-hour candles |
| `TradeVolume` | Order size used for entries. | 0.1 |
| `RsiPeriod` | RSI lookback length. | 14 |
| `RsiUpperLevel` | RSI threshold that signals a bearish reversal. | 60 |
| `RsiLowerLevel` | RSI threshold that signals a bullish reversal. | 20 |
| `TakeProfitPips` | Take-profit distance in pips (0 disables). | 60 |
| `StopLossPips` | Stop-loss distance in pips (0 disables). | 0 |
| `TrailingStopPips` | Trailing-stop distance in pips (0 disables trailing). | 15 |
| `TrailingStepPips` | Minimum price improvement before trailing stop is shifted again. | 5 |

> **Pip interpretation:** In the StockSharp port a "pip" equals one `Security.PriceStep`. On FX symbols with fractional quoting ensure the price step matches the instrument's pip convention, otherwise adjust the input distances accordingly.

## Risk Management
- Take profit and stop loss are evaluated on every completed candle using the latest average position price.
- The trailing stop activates only after the move exceeds `TrailingStopPips + TrailingStepPips` and then trails the close by `TrailingStopPips` as price advances.
- Stop checks use candle highs/lows to emulate intrabar triggers; when triggered the position is closed at market.

## Conversion Notes
- High-level API is used (`SubscribeCandles` + `Bind`), and RSI values are consumed directly from the binding callback without manual indicator buffers.
- Trailing stop logic reproduces the MQL conditions, including the step threshold before each adjustment.
- The strategy resets trailing state whenever exposure flips or closes to avoid stale levels carrying into a new trade.
