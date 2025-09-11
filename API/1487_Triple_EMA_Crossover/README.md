# Triple EMA Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on three simple moving averages.
A long trade is opened when the short SMA crosses above the middle SMA while all three are aligned upward.
A short trade is opened on the opposite crossover and alignment.
Price crossing back over the short SMA exits the position.

## Details

- **Entry Criteria**: Crossovers of SMA1 and SMA2 with trend filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crossing SMA1 or protective stops.
- **Stops**: Yes.
- **Default Values**:
  - `Sma1Period` = 9
  - `Sma2Period` = 21
  - `Sma3Period` = 55
  - `StopLossTicks` = 200
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA
  - Stops: Fixed
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
