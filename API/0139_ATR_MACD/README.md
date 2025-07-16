# ATR MACD Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
ATR MACD uses volatility from the Average True Range to adjust position size while trading MACD crossovers.
Larger ATR readings result in smaller trade size, keeping risk consistent across market regimes.

Entries occur when MACD crosses its signal line, with exits triggered by the opposite crossover or a volatility-based stop.

This combination seeks to capture momentum while accounting for changing volatility.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ATR, MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
