# EMA WMA Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on crossover between exponential moving average (EMA) and weighted moving average (WMA) calculated on candle open prices.
Enters long when EMA crosses below WMA and short when EMA crosses above WMA.
Position size is determined by risk percent of account equity.
The strategy uses fixed take profit and stop loss distances defined in ticks.

## Details

- **Entry Criteria**:
  - Long: `EMA crosses below WMA`
  - Short: `EMA crosses above WMA`
- **Long/Short**: Both
- **Exit Criteria**: Stop loss or take profit
- **Stops**: Yes
- **Default Values**:
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 50
  - `RiskPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Moving average crossover
  - Direction: Both
  - Indicators: EMA, WMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
