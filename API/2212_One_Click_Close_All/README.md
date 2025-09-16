# One Click Close All Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Utility strategy that closes all open positions and optionally cancels pending orders. Useful for quickly flattening an account with a single start command.

## Details

- **Purpose**: Close positions and cancel pending orders
- **Entry Criteria**: None – runs immediately on start
- **Long/Short**: Both, depending on current portfolio
- **Exit Criteria**: Not applicable (stops after execution)
- **Stops**: No
- **Default Values**:
  - `RunOnCurrentSecurity` = true
  - `CloseOnlyManualTrades` = true
  - `DeletePendingOrders` = false
  - `MaxSlippage` = 5
- **Filters**:
  - Category: Utility
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Variable
