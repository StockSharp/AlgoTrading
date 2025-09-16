# Daily Breakpoint Strategy

This strategy trades breakouts from the daily open. At the beginning of each new day the opening price is stored. When the price moves away from this level by a user defined number of points, and the previous bar is within a configurable size range, the strategy enters in the breakout direction.

## Entry logic

- If the previous bar is bullish and price rises above the daily open by **Break Point** points, a long position is opened.
- If the previous bar is bearish and price falls below the daily open by **Break Point** points, a short position is opened.
- Previous bar size must be between **Last Bar Min** and **Last Bar Max** points.
- Breakout level must lie within the previous bar body.

## Risk management

- Optional **Take Profit** and **Stop Loss** are measured in points from the entry price.
- A trailing stop can be enabled with **Trailing Start**, **Trailing Stop** and **Trailing Step** parameters. When price moves in favour by *Trailing Start* the stop is set at *Trailing Stop* points from the entry and then trails by *Trailing Step* increments.

## Parameters

| Name | Description |
| ---- | ----------- |
| Candle Type | Timeframe of processed candles. |
| Break Point | Distance from daily open to trigger a trade (points). |
| Last Bar Min | Minimum size of the previous bar (points). |
| Last Bar Max | Maximum size of the previous bar (points). |
| Trailing Start | Price move to start trailing stop (points). |
| Trailing Stop | Initial trailing distance (points). |
| Trailing Step | Step to move trailing stop (points). |
| Take Profit | Take profit distance (points). |
| Stop Loss | Stop loss distance (points). |

## Notes

The strategy operates on finished candles only and uses market orders for entries and exits. It stores internal variables for previous bar data and trailing stop level.
