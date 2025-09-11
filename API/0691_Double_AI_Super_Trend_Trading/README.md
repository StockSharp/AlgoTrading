# Double AI Super Trend Trading Strategy

This strategy uses two SuperTrend indicators combined with weighted moving averages to confirm trend direction. Long trades open when both SuperTrends are bullish and price WMAs stay above their corresponding SuperTrend WMAs. Short trades occur on the opposite conditions. Positions are managed with an ATR-based trailing stop from the first SuperTrend.

- **Long**: Both SuperTrends bullish and price WMAs above SuperTrend WMAs.
- **Short**: Both SuperTrends bearish and price WMAs below SuperTrend WMAs.
- **Indicators**: SuperTrend, WMA, ATR.
- **Stops**: Trailing stop based on first SuperTrend.
