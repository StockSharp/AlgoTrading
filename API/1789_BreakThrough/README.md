# BreakThrough Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The BreakThrough strategy executes trades when price crosses user-defined trendline levels.
Two main levels are used:
- **Buy Line** – price level to trigger a long position.
- **Sell Line** – price level to trigger a short position.

Once a line is crossed from the opposite side, the strategy enters the market in that direction.
Optional additional lines allow closing a position when price touches a specific level.
Protective stop-loss, take-profit and trailing-stop distances are measured in pips from the entry price.

## Details

- **Entry Criteria**:
  - **Long**: price crosses above or below the Buy Line depending on its initial position.
  - **Short**: price crosses above or below the Sell Line depending on its initial position.
- **Long/Short**: both sides.
- **Exit Criteria**:
  - Price hits an optional take-profit or stop-loss line.
  - Price reaches take-profit or stop-loss distance in pips.
  - Trailing stop is triggered.
- **Stops**: yes, using `StopLossPips`, `TakeProfitPips` and `TrailingStopPips`.
- **Default Values**:
  - `BuyLinePrice` = 0 (disabled)
  - `SellLinePrice` = 0 (disabled)
  - `TakeProfitPips` = 100
  - `StopLossPips` = 30
  - `TrailingStopPips` = 20
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Any (default 1 minute)
  - Risk level: Medium

