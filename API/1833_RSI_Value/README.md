# RSI Value
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades based on the Relative Strength Index (RSI) crossing a middle value.

The idea is to watch for the RSI to cross above or below a configurable level (default 50). When the indicator moves from below to above this level a long position is opened. When it crosses back below the level a short position is opened. Existing positions are exited on the opposite cross. Optional stop-loss, take-profit and trailing stop protect the trade.

## Details

- **Entry Criteria**: Buy when RSI crosses above the level. Sell when RSI crosses below.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite cross or trailing stop.
- **Stops**: Optional fixed stop-loss, take-profit and trailing stop.
- **Default Values**:
  - `RsiPeriod` = 14
  - `RsiLevel` = 50
  - `StopLoss` = 100
  - `TakeProfit` = 200
  - `TrailingStop` = 0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
