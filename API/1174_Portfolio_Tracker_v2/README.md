# Portfolio Tracker v2
[Русский](README_ru.md) | [中文](README_cn.md)

Tracks up to ten positions and a cash balance to show current portfolio value and profit/loss. The strategy listens to candle closes of each configured symbol and logs portfolio statistics. It does not place any orders.

## Details

- **Entry Criteria**: None (tracking only)
- **Long/Short**: Not applicable
- **Exit Criteria**: None
- **Stops**: No
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `UseCash` = true
  - `Cash` = 10000
  - Ten sets of position parameters (enabled flag, symbol, quantity, cost)
- **Filters**:
  - Category: Utility
  - Direction: None
  - Indicators: None
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
