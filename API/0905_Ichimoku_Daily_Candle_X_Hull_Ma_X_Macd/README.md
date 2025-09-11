# Ichimoku Daily Candle X Hull MA X MACD
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines Ichimoku lead lines, daily candle direction, Hull Moving Average trend and an HMA-based MACD. Long positions are opened when all components align bullish; shorts occur when all conditions turn bearish.

## Details

- **Entry Criteria**:
  - **Long**: HMA rising, current price above previous HMA, current daily candle higher than previous, SenkouA > SenkouB, MACD line > signal.
  - **Short**: HMA falling, price below previous HMA, current daily candle lower than previous, SenkouA < SenkouB, MACD line < signal.
- **Long/Short**: Both sides.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `HmaPeriod` = 14
  - `ConversionPeriod` = 9
  - `BasePeriod` = 26
  - `SpanPeriod` = 52
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `PriceSource` = Open
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Ichimoku, Hull MA, MACD
  - Stops: None
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
