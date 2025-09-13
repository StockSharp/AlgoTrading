# Liquidex Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy that enters when price moves outside Keltner Channel bands and manages risk with stop loss, take profit, break-even and trailing stop.

## Details

- **Entry Criteria**:
  - Long: close above the upper Keltner band.
  - Short: close below the lower Keltner band.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Stop loss or take profit level reached.
  - Stop moved to break-even after profit target.
  - Trailing stop activated.
- **Stops**: Yes.
- **Default Values**:
  - `KcPeriod` = 10
  - `UseKcFilter` = true
  - `StopLoss` = 30
  - `TakeProfit` = 0
  - `MoveToBe` = 15
  - `MoveToBeOffset` = 2
  - `TrailingDistance` = 5
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Channel
  - Direction: Both
  - Indicators: Keltner
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
