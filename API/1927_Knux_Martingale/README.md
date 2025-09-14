# Knux Martingale Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Martingale strategy that increases trade volume after a losing position. The method filters entries by Average Directional Index (ADX) to trade only in trending markets. Bullish candles open long positions, bearish candles open short positions.

## Details

- **Entry Criteria**:
  - ADX > 25
  - Long: `Close > Open`
  - Short: `Close < Open`
- **Long/Short**: Both
- **Exit Criteria**: Stop loss or take profit
- **Stops**: Yes
- **Default Values**:
  - `AdxPeriod` = 14
  - `LotsMultiplier` = 1.5m
  - `StopLoss` = 150m
  - `TakeProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following, Martingale
  - Direction: Both
  - Indicators: AverageDirectionalIndex
  - Stops: Absolute
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
