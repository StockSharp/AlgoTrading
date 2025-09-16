# Ilan Dynamic HT Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Grid-based martingale strategy that opens positions based on RSI signals and expands the position using a dynamic price range. Each additional trade increases volume by a multiplier and shares the same take profit and stop loss.

## Details

- **Entry Criteria**:
  - Long: RSI below `RsiMinimum`
  - Short: RSI above `RsiMaximum`
- **Long/Short**: Long and Short
- **Exit Criteria**:
  - Common take profit or stop loss is reached
- **Stops**:
  - `TakeProfit` in points
  - `StopLoss` in points
- **Default Values**:
  - `LotExponent` = 1.4
  - `MaxTrades` = 10
  - `DynamicPips` = true
  - `DefaultPips` = 120
  - `Depth` = 24
  - `Del` = 3
  - `BaseVolume` = 0.1
  - `RsiPeriod` = 14
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `TakeProfit` = 100
  - `StopLoss` = 500
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Grid / Martingale
  - Direction: Long and Short
  - Indicators: RSI, Highest, Lowest
  - Stops: Take Profit, Stop Loss
  - Complexity: Advanced
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
