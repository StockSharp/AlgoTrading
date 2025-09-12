# Percent Stop Take Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses two simple moving averages (SMA) to detect trend direction. When the fast SMA crosses above the slow SMA, it enters a long position. When the fast SMA crosses below the slow SMA, it enters a short position. After entering, the strategy sets stop-loss and take-profit levels as percentages of the entry price.

## Details

- **Entry Criteria**:
  - **Long**: Fast SMA crosses above Slow SMA.
  - **Short**: Fast SMA crosses below Slow SMA.
- **Exit Criteria**:
  - Stop-loss and take-profit based on percentages of entry price.
- **Stops**: Yes, both stop-loss and take-profit.
- **Indicators**: SMA.
- **Category**: Trend following.
- **Timeframe**: Any.
