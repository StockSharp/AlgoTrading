# Four WMA Strategy with TP and SL
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using crossover of four moving averages with optional take profit, stop loss, and alternate exit condition.

## Details

- **Entry Criteria**:
  - Long: Long MA1 crosses above Long MA2
  - Short: Short MA1 crosses below Short MA2
- **Long/Short**: Configurable
- **Stops**: Percent-based TP and SL
- **Default Values**:
  - `LongMa1Length` = 10
  - `LongMa2Length` = 20
  - `ShortMa1Length` = 30
  - `ShortMa2Length` = 40
  - `MaType` = Wma
  - `EnableTpSl` = true
  - `TakeProfitPercent` = 1m
  - `StopLossPercent` = 1m
  - `Direction` = Both
  - `EnableAltExit` = false
  - `AltExitMaOption` = LongMa1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Moving Averages
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
