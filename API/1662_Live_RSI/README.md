# Live RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses multiple RSI calculations (close, weighted, typical, median, open) and Parabolic SAR to detect trend reversals. Enters long when RSI values align in bullish order and price is above SAR, enters short when alignment is bearish and price is below SAR. SAR value acts as a trailing stop.

## Details

- **Entry Criteria**:
  - Long when RSI sequence is bullish and price is above SAR.
  - Short when RSI sequence is bearish and price is below SAR.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite trend signal or SAR trailing stop.
- **Stops**: Optional fixed stop loss plus SAR-based trailing stop.
- **Default Values**:
  - `RSI Period` = 30
  - `SAR Step` = 0.08
  - `Stop Loss` = 40
  - `Check Hour` = false
  - `Start Hour` = 17
  - `End Hour` = 1
  - `Candle Type` = 1 hour
- **Filters**:
  - Category: Trend Following
  - Direction: Long & Short
  - Indicators: RSI, Parabolic SAR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Optional (time filter)
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
