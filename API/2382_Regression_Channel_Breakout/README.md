# Regression Channel Breakout Strategy

This strategy implements a regression channel trading system based on the MQL script `e-Regr`.
It builds a linear regression line over a configurable number of recent candles and
adds upper and lower bands at a specified standard deviation distance. Trading rules:

- **Long Entry:** when the candle low touches or breaks below the lower band.
- **Short Entry:** when the candle high touches or breaks above the upper band.
- **Exit:** when the closing price crosses the regression line in the opposite direction.
- **Trailing Stop:** optional trailing logic moves the stop level after the trade
  has reached a configured profit.

## Parameters

| Name            | Description                                                     |
|-----------------|-----------------------------------------------------------------|
| `CandleType`    | Candle type used for calculations.                              |
| `Length`        | Number of candles for regression and standard deviation.        |
| `Deviation`     | Standard deviation multiplier for channel width.                |
| `UseTrailing`   | Enables trailing stop logic.                                    |
| `TrailingStart` | Profit required before trailing starts.                         |
| `TrailingStep`  | Distance between price and trailing stop.                       |

The strategy uses the high-level StockSharp API via `SubscribeCandles` and `Bind`
methods to receive candle data and indicator values.
