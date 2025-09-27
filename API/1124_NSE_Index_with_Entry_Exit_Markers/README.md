# NSE Index Strategy with Entry Exit Markers
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy goes long when price is above a trend SMA and the RSI crosses above an oversold level. An ATR-based stop loss and take profit manage the position.

## Details

- **Entry Criteria**:
  - **Long**: price is above the SMA and RSI crosses upward above the oversold level.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - close the long position when price hits ATR-based stop or take profit.
- **Stops**: ATR-based stop loss and take profit.
- **Default Values**:
  - `SmaPeriod` = 200.
  - `RsiPeriod` = 14.
  - `RsiOversold` = 40.
  - `AtrPeriod` = 14.
  - `AtrMultiplier` = 1.5.
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame().
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: SMA, RSI, ATR
  - Stops: ATR-based
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
