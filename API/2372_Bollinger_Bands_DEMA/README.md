# Bollinger Bands with DEMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines Bollinger Bands calculated on 30-minute candles with a Double Exponential Moving Average (DEMA) from daily data to trade breakouts with trend confirmation.

A long setup occurs when a bullish candle crosses above the lower Bollinger Band while the DEMA is rising, confirming upward momentum. A short setup occurs when a bearish candle crosses below the upper Bollinger Band while the DEMA is falling. Positions are closed when an opposite colored candle crosses the outer band against the trade.

## Details

- **Entry Criteria**:
  - **Long**: Candle closes above the lower band and opens below it AND daily DEMA is increasing for three consecutive days.
  - **Short**: Candle closes below the upper band and opens above it AND daily DEMA is decreasing for three consecutive days.
- **Long/Short**: Both.
- **Exit Criteria**:
  - **Long**: Bearish candle closes below the upper band after opening above it.
  - **Short**: Bullish candle closes above the lower band after opening below it.
- **Stops**: None.
- **Default Values**:
  - `BollingerPeriod` = 20
  - `DemaPeriod` = 20
  - `Deviation` = 2
  - `CandleType` = 30-minute timeframe
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands, DEMA
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Intraday with daily trend filter
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
