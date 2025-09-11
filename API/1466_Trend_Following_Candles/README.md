# Trend Following Candles Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy follows the trend using a moving average and simple candlestick signals.
It buys when price is above the moving average with a bullish candle breaking pivot resistance, and sells when price is below the moving average with a bearish candle breaking pivot support.

## Details

- **Entry Criteria**: bullish/bearish candle above/below MA breaking pivot levels
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `MaPeriod` = 10
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
