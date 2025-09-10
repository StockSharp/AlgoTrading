# Built-in Kelly Ratio
[Русский](README_ru.md) | [中文](README_cn.md)

Channel breakout strategy using a moving average and ATR bands with position sizing based on the Kelly ratio.

## Details

- **Entry Criteria**: Price crossing above or below ATR-based bands.
- **Long/Short**: Both.
- **Exit Criteria**: Optional take profit and stop loss.
- **Stops**: Optional.
- **Default Values**:
  - `Length` = 20
  - `Multiplier` = 1
  - `AtrLength` = 10
  - `UseEma` = true
  - `UseKelly` = true
  - `UseTakeProfit` = false
  - `UseStopLoss` = false
  - `TakeProfit` = 10
  - `StopLoss` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: MA, ATR
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
