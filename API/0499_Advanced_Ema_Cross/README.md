# Advanced EMA Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy goes long when a short-term EMA crosses above a long-term EMA while filtering signals with normalized ATR, ADX trend strength and a SuperTrend direction check. Stop-loss and take-profit levels adapt based on USD strength inferred from a 50-period EMA.

## Details

- **Entry Criteria**:
  - Short EMA crosses above long EMA.
  - Normalized ATR above thresholds depending on trend direction.
  - SuperTrend confirms bull or bear market.
- **Exit Criteria**:
  - Opposite EMA cross or ADX above threshold after a minimum holding period.
  - Stop-loss or take-profit hit.
- **Indicators**: EMA, ATR, ADX, SuperTrend, SMA (volume).
- **Stops**: Dynamic percentage stop-loss and take-profit.
- **Type**: Trend following.
- **Timeframe**: 30 minutes (default).

