# Volatility Arbitrage Spread Oscillator Model (VASOM)
[Русский](README_ru.md) | [中文](README_cn.md)

Longs the front month VIX future when the RSI of the spread between the first and second month contracts drops below a threshold. The position is closed when the RSI rises above an exit level.

## Details
- **Entry Criteria**: Spread RSI < `LongThreshold`.
- **Long/Short**: Long only.
- **Exit Criteria**: Spread RSI > `ExitThreshold`.
- **Stops**: No.
- **Default Values**:
  - `RsiPeriod` = 2
  - `LongThreshold` = 46
  - `ExitThreshold` = 76
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SecondSecurity` = "CBOE:VX2!"
- **Filters**:
  - Category: Volatility
  - Direction: Long
  - Indicators: RSI
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
