# Exp Super Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy converted from MQL script **Exp_Super_Trend.mq5** (ID 14269). It follows the direction of the SuperTrend indicator and reverses positions whenever the trend flips. The implementation uses StockSharp high level API and the built-in SuperTrend indicator.

The indicator calculates a dynamic support or resistance line based on ATR. When price stays above this line the trend is considered bullish, otherwise bearish. The strategy opens a long position during bullish periods and switches to a short position during bearish periods. Each flip of the indicator causes an immediate position reversal.

This approach works best in trending markets where large moves follow a breakout. It is also useful as an educational template showing how to connect an indicator using `BindEx` and execute market orders on completed candles.

## Details

- **Entry Criteria**:
  - Long: SuperTrend signals an uptrend.
  - Short: SuperTrend signals a downtrend.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal from SuperTrend (position is reversed).
- **Stops**: No explicit stop-loss; the indicator line acts as a trailing stop.
- **Default Values**:
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SuperTrend
  - Stops: Indicator based
  - Complexity: Basic
  - Timeframe: Medium (1 hour by default)
  - Seasonality: None
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
