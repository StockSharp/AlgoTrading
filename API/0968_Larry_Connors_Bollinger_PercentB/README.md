# Larry Connors Percent B Bollinger
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy follows the Larry Connors %B approach. It buys when price is in an uptrend above the 200-period SMA and the Bollinger %B value stays below a threshold for three consecutive candles. Positions are closed when %B rises above an upper threshold.

The default configuration targets daily candles.

## Details

- **Entry Criteria**: Close above SMA200 and %B below `LowPercentB` for three consecutive candles.
- **Long/Short**: Long only.
- **Exit Criteria**: %B crosses above `HighPercentB` or stop.
- **Stops**: Yes.
- **Default Values**:
  - `SmaPeriod` = 200
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `LowPercentB` = 0.2m
  - `HighPercentB` = 0.8m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend Following
  - Direction: Long
  - Indicators: Bollinger Bands, SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
