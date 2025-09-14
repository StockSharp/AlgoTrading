# Grid Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements a basic grid trading system. It places buy stop and sell stop orders at fixed price intervals defined by `GridStep`. Each executed order uses a fixed take profit distance. A global profit target closes all positions and resets the grid. Optionally, the volume of new orders increases following a martingale scheme.

## Details

- **Entry Criteria:**
  - Buy stop one step above the last price.
  - Sell stop one step below the last price.
- **Long/Short:** Both.
- **Exit Criteria:**
  - Each position closes at the fixed take profit.
  - When total profit exceeds `ProfitTarget` all orders and positions are closed.
- **Stops:** Take profit only.
- **Filters:** None.
