# Divergence Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy compares two simple moving averages (SMA) and trades based on the divergence between them.

It uses the difference between the fast and slow SMA from the previous candle as a divergence measure. If this divergence is positive but within a specified range, the strategy opens a long position. If the divergence is negative and within the mirrored range, it opens a short position. Risk is managed through optional stop-loss and take-profit levels.

## Details

- **Entry Criteria**:
  - **Long**: Previous fast SMA - previous slow SMA >= `DvBuySell` and <= `DvStayOut`.
  - **Short**: Previous fast SMA - previous slow SMA <= `-DvBuySell` and >= `-DvStayOut`.
- **Exit Criteria**: Positions are closed via stop-loss or take-profit if configured.
- **Stops**: Supported via `StartProtection` with absolute price offsets.
- **Default Values**:
  - `FastPeriod` = 7
  - `SlowPeriod` = 88
  - `DvBuySell` = 0.0011
  - `DvStayOut` = 0.0079
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
