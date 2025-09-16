# JK BullP AutoTrader
[Русский](README_ru.md) | [中文](README_cn.md)

The JK BullP AutoTrader is a port of the original MetaTrader expert advisor that relies on the Bulls Power oscillator. It interprets the relationship between two consecutive Bulls Power values to detect when bullish strength is fading above the zero line or when it drops below zero and reverses. Long and short trades are protected with fixed stops and an incremental trailing stop that tightens as the trade becomes profitable.

## Details

- **Entry Criteria**: Sell when Bulls Power two bars ago is above the previous bar and the previous bar is above zero. Buy when the previous Bulls Power bar is below zero.
- **Long/Short**: Both.
- **Exit Criteria**: Fixed take profit, fixed stop loss, or trailing stop hit. Opposite signals reverse the position.
- **Stops**: Fixed take profit, fixed stop loss, trailing stop.
- **Default Values**:
  - `BullsPeriod` = 13
  - `TakeProfitPoints` = 350
  - `StopLossPoints` = 100
  - `TrailingStopPoints` = 100
  - `TrailingStepPoints` = 40
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Bulls Power
  - Stops: Fixed + Trailing
  - Complexity: Basic
  - Timeframe: Intraday / Swing (1H)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
