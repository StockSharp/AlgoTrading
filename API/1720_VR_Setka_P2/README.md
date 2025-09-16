# VR Setka P2 Strategy

This strategy is a grid-based approach translated from the MetaTrader 4 expert `VR---SETKAp2`.
It trades when the daily close deviates from the day's high or low by a given percentage.
The strategy opens long positions after a significant drop from the daily high and
short positions after a significant rise from the daily low. Once in a position it
exits at a fixed take-profit distance. Volume can optionally increase using a simple
martingale scheme.

## Parameters
- **TakeProfit** – distance to the profit target in price steps.
- **Lot** – base volume for every trade.
- **Percent** – percentage threshold calculated from the daily range.
- **UseMartingale** – enable volume increase when adding to a losing position.
- **Slippage** – allowed price slippage for orders.
- **Correlation** – offset applied when calculating grid levels.
- **Candle Type** – timeframe used for calculations (daily by default).

## Logic
1. Subscribe to daily candles.
2. For each finished candle, calculate percentage deviations from the daily high and low.
3. Enter long or short depending on the deviation and the previous candle direction.
4. Close the position when the take-profit level is reached.

This implementation demonstrates how a classic MetaTrader grid expert can be
ported to the StockSharp high-level API.
