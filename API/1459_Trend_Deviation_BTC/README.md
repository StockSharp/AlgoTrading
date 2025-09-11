# Trend Deviation BTC
[Русский](README_ru.md) | [中文](README_cn.md)

Combines DMI crosses with Bollinger Bands and confirmations from momentum, MACD, SuperTrend and Aroon. The strategy looks for price deviations within a trend and enters when multiple signals align.

## Details

- **Entry Criteria**: +DI crossing above -DI, price below Bollinger upper band and any momentum/MACD/SuperTrend/Aroon confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `DmiPeriod` = 15
  - `BbLength` = 13
  - `BbMultiplier` = 2.3
  - `MomentumLength` = 10
  - `AroonLength` = 5
  - `MacdFast` = 15
  - `MacdSlow` = 200
  - `MacdSignal` = 25
  - `AtrPeriod` = 200
  - `SuperTrendFactor` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: DMI, Bollinger Bands, Momentum, MACD, SuperTrend, Aroon
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
