# Multi-TF AI SuperTrend with ADX Strategy

This strategy combines two SuperTrend indicators filtered by an ADX strength check. Trend direction is confirmed by comparing price WMAs with SuperTrend WMAs. Long trades open when both SuperTrends are bullish and ADX shows positive strength. Short trades open under opposite conditions. The first SuperTrend's ATR provides a trailing stop.

- **Long**: Both SuperTrends bullish, price WMAs above SuperTrend WMAs, +DI > -DI and ADX above threshold.
- **Short**: Both SuperTrends bearish, price WMAs below SuperTrend WMAs, -DI > +DI and ADX above threshold.
- **Indicators**: SuperTrend, WMA, ATR, ADX.
- **Stops**: ATR-based trailing stop from the first SuperTrend.
