# Plateau Strategy

The Plateau strategy is a conversion of the original MetaTrader 5 expert advisor. It combines a pair of linear-weighted moving averages with Bollinger Bands to detect potential reversals when price trades near the lower band.

## Trading idea

* Calculate fast and slow moving averages using the selected smoothing method and price source.
* Build Bollinger Bands around the same price series.
* When the fast average crosses above the slow average while the previous candle closed below the lower band, open a long position.
* When the fast average crosses below the slow average while the previous candle closed above the lower band, open a short position.
* Optionally reverse the signals if the `Reverse` switch is enabled.

## Order management

* Positions can be sized either with a fixed lot or by risking a percentage of the portfolio value per trade.
* Stop-loss and take-profit levels are expressed in pips and immediately attached after the market order is filled.
* A trailing stop can be activated when both trailing distance and step are positive.
* When `Close Opposite` is enabled the strategy automatically closes the opposing position before entering a new trade.

## Parameters

| Parameter | Description |
| --- | --- |
| Stop Loss | Stop-loss distance in pips. |
| Take Profit | Take-profit distance in pips. |
| Trailing Stop | Trailing stop distance in pips. |
| Trailing Step | Minimal increment (in pips) required to move the trailing stop. |
| Money Mode | Choose between fixed lot and risk percentage sizing. |
| Lot / Risk | Either the fixed lot size or the risk percentage depending on the selected money mode. |
| Fast MA / Slow MA | Periods for the moving average pair. |
| MA Shift | Horizontal shift applied to both moving averages. |
| MA Method | Moving average smoothing algorithm. |
| MA Price | Price source used for moving average calculations. |
| Bands Period | Averaging period for Bollinger Bands. |
| Bands Shift | Horizontal shift applied to Bollinger Band values. |
| Bands Deviation | Standard deviation multiplier for Bollinger Bands. |
| Bands Price | Price source used for Bollinger Bands calculations. |
| Reverse | Invert the long and short signal logic. |
| Close Opposite | Close an existing position in the opposite direction before opening a new one. |
| Verbose Log | Print detailed execution information to the log. |
| Candle Type | Candle data series used for indicator calculations. |

## Notes

* The pip size is automatically adjusted to instruments with three or five decimal digits to match the original expert behaviour.
* When the trailing stop is enabled the trailing step must be strictly positive, otherwise the strategy throws an error on start.
* Risk-based position sizing requires both a valid stop-loss distance and portfolio valuation data. When unavailable the strategy falls back to the default volume.
