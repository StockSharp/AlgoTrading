# Limit Orders Control Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy monitors the number of active pending orders (limit and stop) for the current instrument. It is intended to supervise externally created orders and stop trading if the order grid collapses.

The strategy subscribes to the trade stream and counts pending orders after each tick. If fewer than two pending orders remain, it cancels all of them. When no pending orders are left, the strategy stops its work and writes a log message.

## Parameters

- **Magic Number** – identifier of orders. Preserved for compatibility with the original MQL version; not used in StockSharp.
- **Write Comments** – when `true`, the strategy logs information about the number of active orders and its state.

## Details

- **Entry Criteria**: none; orders are assumed to be placed externally.
- **Long/Short**: both, depending on existing pending orders.
- **Exit Criteria**: strategy stops when all pending orders are canceled.
- **Stops**: not used.
- **Default Values**:
  - `Magic Number` = 0
  - `Write Comments` = true
- **Filters**:
  - Category: Utility
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
