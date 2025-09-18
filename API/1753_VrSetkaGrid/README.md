# VR Setka Grid Strategy

This strategy is a StockSharp implementation of the MetaTrader "VR---SETKAa3hM" grid system. It opens a sequence of buy or sell orders based on percentage deviation from the daily range and optionally increases volume using a martingale multiplier. The average entry price of all open orders is tracked to place a unified take-profit target.

## Parameters
- `Distance`: Price distance in points between grid levels.
- `TakeProfit`: Profit target in points for the initial order.
- `Correction`: Extra profit in points added to the average price when more than one order is open.
- `SignalPercent`: Percentage threshold used to detect deviation from the daily range.
- `UseMartingale`: Multiply volume by the number of open orders.
- `CandleType`: Candle timeframe used for signal calculations.

## Logic
1. When a finished candle appears, compute the current close in relation to the day high and low.
2. If the previous candle was bullish and the close is sufficiently below the day high, start or continue a buy grid.
3. If the previous candle was bearish and the close is sufficiently above the day low, start or continue a sell grid.
4. Additional orders are placed whenever price moves against the position by `Distance` points.
5. Once price returns to the average entry price plus `Correction` for buys or minus `Correction` for sells, all positions are closed with a market order.
