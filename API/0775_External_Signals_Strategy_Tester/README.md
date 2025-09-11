# External Signals Strategy Tester
[Русский](README_ru.md) | [中文](README_cn.md)

Template strategy that executes trades based on external long and short signals crossing above zero. Supports optional position reversal, closing on opposite signal, and percent-based take-profit, stop-loss, and break-even.

## Details

- **Entry Criteria**: Long or short signal series crosses above zero within date range.
- **Long/Short**: Both.
- **Exit Criteria**: Take-profit, stop-loss or break-even stop.
- **Stops**: Yes.
- **Default Values**:
  - `StartDate` = 2024-11-01 00:00:00
  - `EndDate` = 2025-03-31 23:59:00
  - `EnableLong` = true
  - `EnableShort` = true
  - `CloseOnReverse` = true
  - `ReversePosition` = false
  - `TakeProfitPerc` = 2
  - `StopLossPerc` = 1
  - `BreakevenPerc` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Signals
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
