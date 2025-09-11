# Ichimoku RSI MACD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy combining Ichimoku Cloud, RSI and MACD crossover signals.

## Details

- **Entry Criteria**: Price above/below the Ichimoku cloud with RSI filter and MACD line crossing the signal line.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite MACD crossover.
- **Stops**: None.
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: Ichimoku, RSI, MACD
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday (1h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
