# Universal Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the core idea of the original MQL4 script `cm_universal_trailing_stop.mq4`. It does not generate entry signals; instead it manages an existing position by moving the stop-loss in the direction of profit.

The algorithm keeps an offset from the current price and shifts the stop every time the market moves by a configurable step. Once the minimum profit threshold is reached, the trailing stop becomes active and follows price automatically for both long and short positions.

## Details

- **Entry Criteria**: none. Position should be opened manually or by another strategy.
- **Long/Short**: both.
- **Exit Criteria**: stop order hit when price reverses by the configured offset.
- **Stops**: trailing stop based on points.
- **Parameters**:
  - `Delta` – distance from price to stop in points.
  - `Step` – minimum price movement in points to shift the stop.
  - `StartProfit` – profit in points required to activate trailing.
  - `CandleType` – timeframe used for trailing calculations.
- **Filters**:
  - Category: Risk management
  - Direction: Both
  - Indicators: None
  - Stops: Trailing
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
