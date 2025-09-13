# ZMFX Stolid 5a EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Multi-timeframe trend-following strategy that enters on pullbacks confirmed by RSI and Stochastic readings.
The system identifies the main trend from 4-hour Stochastic and 1-hour smoothed moving averages.
Positions are opened on candle reversals with oversold/overbought RSI conditions and closed on opposing signals.

## Details

- **Entry Criteria**:
  - Long: `UpTrend && PreviousBarDown && PrevRSI < 30 && (RSI15 < 30 => double volume)`
  - Short: `DownTrend && PreviousBarUp && PrevRSI > 70 && (RSI15 > 70 => double volume)`
- **Long/Short**: Both
- **Stops**: No explicit stops; positions closed by indicator conditions
- **Default Values**:
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSI, Stochastic, Smoothed Moving Average
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
