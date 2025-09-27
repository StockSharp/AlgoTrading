# Smart Fib Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using a simple moving average breakout for entries and ATR-based Fibonacci bands for exits.

## Details

- **Entry Criteria**: Close crossing above or below SMA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price reaching ATR Fibonacci band.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SmaLength` = 50
  - `FibSmaLength` = 8
  - `AtrLength` = 6
  - `FirstFactor` = 1.618
  - `SecondFactor` = 2.618
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA, ATR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
