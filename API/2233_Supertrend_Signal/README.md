# Supertrend Signal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy opens positions when the closing price crosses the SuperTrend line. A long trade is placed when price moves above the line, and a short trade is opened when price drops below it. Opposite signals close and reverse existing positions.

The SuperTrend indicator uses the Average True Range (ATR) to follow price and define the prevailing trend. Parameters allow configuring ATR period, multiplier and candle timeframe.

## Details

- **Entry Criteria**:
  - Long: Close price crosses above SuperTrend
  - Short: Close price crosses below SuperTrend
- **Long/Short**: Long and Short
- **Exit Criteria**:
  - Opposite SuperTrend crossover
- **Stops**: None
- **Default Values**:
  - `AtrPeriod` = 5
  - `Multiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SuperTrend (ATR-based)
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Mid-term
  - Seasonality: None
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
