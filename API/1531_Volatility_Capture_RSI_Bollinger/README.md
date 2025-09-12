# Volatility Capture RSI-Bollinger
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combines dynamic Bollinger bands with an optional RSI filter to catch volatility swings.

## Details
- **Entry Criteria**: Price crossing adaptive Bollinger band with optional RSI confirmation.
- **Long/Short**: Configurable via `Direction`.
- **Exit Criteria**: Price crossing opposite of the trailing band.
- **Stops**: No.
- **Default Values**:
  - `BollingerLength` = 50
  - `Multiplier` = 2.7183m
  - `UseRsi` = true
  - `RsiPeriod` = 10
  - `RsiSmaPeriod` = 5
  - `BoughtRangeLevel` = 55m
  - `SoldRangeLevel` = 50m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Volatility
  - Direction: Configurable
  - Indicators: Bollinger, RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
