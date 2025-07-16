# MACD Trend
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on MACD indicator

MACD Trend reacts to crossovers between the MACD line and its signal line. Bullish crosses initiate longs while bearish crosses start shorts. Opposite crosses or a stop close the trade.

The moving-average convergence divergence indicator adapts well to shifting markets by measuring momentum. This approach aims to ride trending swings while the indicator maintains a clear bullish or bearish bias.


## Details

- **Entry Criteria**: Signals based on MA, MACD.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA, MACD
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
