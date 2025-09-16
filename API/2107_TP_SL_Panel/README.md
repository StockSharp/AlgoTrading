# TP SL Panel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates a simple take-profit/stop-loss panel. It does not place pending orders. Instead, it monitors the current position and closes it with market orders when price reaches the specified levels.

## Details
- **Entry Criteria**:
  - No automatic entries. The strategy only manages an existing position.
- **Long/Short**: Depends on current position.
- **Exit Criteria**:
  - **Long**: Close when price >= `TakeProfitPrice` or price <= `StopLossPrice`.
  - **Short**: Close when price <= `TakeProfitPrice` or price >= `StopLossPrice`.
- **Stops**: Yes, price-based levels.
- **Default Values**:
  - `TakeProfitPrice` = 0 (disabled)
  - `StopLossPrice` = 0 (disabled)
- **Filters**:
  - Category: Utility
  - Direction: Depends on position
  - Indicators: None
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Low
