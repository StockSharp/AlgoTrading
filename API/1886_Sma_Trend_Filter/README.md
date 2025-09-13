# SMA Trend Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Multi-timeframe strategy that analyzes the slope of five simple moving averages (periods 5, 8, 13, 21, 34) on three timeframes (15m, 1h, 4h). It calculates bullish and bearish scores for each timeframe and trades when all timeframes align in one direction.

## Details

- **Entry Criteria**:
  - Long: all three timeframes show at least 50% of SMAs rising
  - Short: all three timeframes show at least 50% of SMAs falling
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal based on close level
- **Stops**: No
- **Default Values**:
  - `OpenLevel` = 0
  - `CloseLevel` = 0
  - `CandleType1` = TimeSpan.FromMinutes(15).TimeFrame()
  - `CandleType2` = TimeSpan.FromHours(1).TimeFrame()
  - `CandleType3` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Trend-following
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
