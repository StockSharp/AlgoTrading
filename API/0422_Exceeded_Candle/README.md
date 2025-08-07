# Exceeded Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This pattern-based approach searches for bullish engulfing candles that exceed the prior bar while price is still below the middle Bollinger Band. The idea is that a strong reversal within a pullback may propel price back toward the upper band. The strategy only trades long and cancels entries when three consecutive bearish candles appear.

Whenever price tags the upper Bollinger Band the position is closed, capturing the quick rebound. The method suits short timeframes where volatility bands capture mean-reversion swings.

## Details

- **Entry Criteria**:
  - **Long**: previous candle red, current candle green and closes above previous open, `Close < MiddleBand`, no three consecutive red candles
- **Long/Short**: Long only
- **Exit Criteria**:
  - **Long**: `Close > UpperBand`
- **Stops**: None
- **Default Values**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
- **Filters**:
  - Category: Mean reversion
  - Direction: Long only
  - Indicators: Bollinger Bands, price action
  - Stops: No
  - Complexity: Low
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
