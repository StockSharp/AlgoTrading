# 30-Minute Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This approach compares the current 30-minute candle opening price with the previous candle's close.
If a new candle opens above the prior close, a long position is opened.
When already long and the next candle gaps down below the previous close, the strategy reverses to a short position.
All open positions are closed one minute before the current candle finishes.

## Details

- **Entry Criteria**:
  - **Long**: current candle open > previous candle close.
  - **Short**: current candle open < previous candle close while holding a long position.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Close any position one minute before the candle closes.
- **Stops**: None.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame().
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Price action
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
