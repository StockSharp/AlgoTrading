# Mikul's Ichimoku Cloud v2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy using Ichimoku Cloud with an optional moving average filter. Positions are managed by a trailing stop (ATR, percent, or Ichimoku rules) and optional take profit.

## Details

- **Entry Criteria**: Tenkan-sen crossing above Kijun-sen with price above the cloud, or a strong breakout above a green cloud.
- **Long/Short**: Long only.
- **Exit Criteria**: Trailing stop or Ichimoku reversal, optional take profit.
- **Stops**: Trailing.
- **Default Values**:
  - `TrailSource` = `LowsHighs`
  - `TrailMethod` = `Atr`
  - `TrailPercent` = 10
  - `SwingLookback` = 7
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1
  - `AddIchiExit` = false
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 25
  - `UseMaFilter` = false
  - `MaType` = `Ema`
  - `MaLength` = 200
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouBPeriod` = 52
  - `Displacement` = 26
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: Ichimoku, ATR
  - Stops: Trailing
  - Complexity: Medium
  - Timeframe: Intraday (1h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
