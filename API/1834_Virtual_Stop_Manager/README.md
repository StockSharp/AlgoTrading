# Virtual Stop Manager

Strategy converted from MetaTrader advisor "VR---STEALS-3-EN". Implements hidden order management features: stop loss, take profit, trailing stop and breakeven. The strategy opens a long position on first candle and manages exit levels virtually without placing visible stop orders on the exchange.

## Parameters
- **Volume**: order volume.
- **Take Profit (points)**: distance in points to close the position with profit.
- **Stop Loss (points)**: distance in points to close the position with loss.
- **Trailing Stop (points)**: distance for trailing stop from the highest price.
- **Breakeven (points)**: profit in points after which stop loss moves to entry price.
- **Candle Type**: candle series used for processing.
