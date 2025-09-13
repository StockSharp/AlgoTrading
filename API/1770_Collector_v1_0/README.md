# Collector v1.0 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy opens market orders when price reaches dynamic buy or sell levels spaced by a fixed distance. Volume increases after a specified number of trades. All positions are closed once cumulative profit exceeds a threshold.

## Details

- **Entry Criteria**:
  - Long: close price >= buy level
  - Short: close price <= sell level
- **Long/Short**: Both
- **Exit Criteria**:
  - Close all when total profit >= ProfitClose
- **Stops**: None
- **Default Values**:
  - `Distance` = 10m
  - `InitialVolume` = 0.01m
  - `VolumeStep` = 0.01m
  - `IncreaseTrade` = 3
  - `MaxTrades` = 200
  - `ProfitClose` = 500000m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Grid
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
