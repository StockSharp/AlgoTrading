# 80-20 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy detects candles where price closes in the top or bottom 20% of the session. A bullish signal occurs when the close is within the upper fifth and the open is within the lower fifth of the range. A bearish signal occurs when the open is within the upper fifth and the close is within the lower fifth. The approach aims to capture rapid reversals from extreme candle closes.

## Details

- **Entry Criteria**:
  - Close in top 20% and open in bottom 20% → long.
  - Open in top 20% and close in bottom 20% → short.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal reverses the position.
- **Stops**: None.
- **Default Values**:
  - Range percent = 0.2.
- **Filters**:
  - Category: Pattern recognition
  - Direction: Both
  - Indicators: None
  - Stops: None
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
