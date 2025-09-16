# ASCTrendND Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is inspired by the ASCTrendND MQL5 expert advisor. It uses a Simple Moving Average as the main trend signal, an RSI filter to confirm strength and an ATR-based trailing stop to exit trades. The approach tries to replicate ASCTrend + NRTR + TrendStrength logic in a simplified form on StockSharp high level API.

## Details

- **Entry Criteria:**
  - **Long:** Close price is above the SMA and RSI > 50.
  - **Short:** Close price is below the SMA and RSI < 50.
- **Exit Criteria:**
  - Trailing stop based on ATR * multiplier or opposite signal.
- **Stops:** ATR-based trailing stop only.
- **Default Values:**
  - `SmaPeriod` = 50
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0
  - `CandleType` = 5-minute candles
- **Filters:**
  - Category: Trend following
  - Direction: Long/Short
  - Indicators: SMA, RSI, ATR
  - Stops: Trailing stop
  - Complexity: Low
  - Timeframe: 5m
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
