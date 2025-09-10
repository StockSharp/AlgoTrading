# Bbsr Extreme
[Русский](README_ru.md) | [中文](README_cn.md)

The **Bbsr Extreme** strategy combines Bollinger Bands breakouts with a trend filter based on a moving average.
A long position appears when price rebounds from the lower band while the average is rising.
A short position is opened on a pullback from the upper band when the average declines.
Exits rely on ATR-based stop loss and take profit.

## Details
- **Entry Criteria**: Price crosses bands with trend confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR stop or take profit.
- **Stops**: Yes, ATR based.
- **Default Values**:
  - `BollingerPeriod = 20`
  - `BollingerMultiplier = 2`
  - `MaLength = 7`
  - `AtrLength = 14`
  - `AtrStopMultiplier = 2`
  - `AtrProfitMultiplier = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Bollinger Bands, EMA, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
