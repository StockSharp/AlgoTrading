# Close Orders Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy immediately closes existing positions and cancels pending orders according to user-defined filters. It can operate on the attached security only or on all portfolio securities. Optional time window and price range restrictions allow precise control over which orders are affected.

## Details

- **Purpose**: risk management and manual liquidation.
- **Operation**:
  - On start the strategy checks the optional time window.
  - If allowed it closes positions and cancels orders matching the filters.
  - After processing the strategy stops automatically.
- **Filters**:
  - `CloseAllSecurities` – include all portfolio instruments instead of only the attached security.
  - `CloseOpenLongOrders` / `CloseOpenShortOrders` – close existing long or short positions.
  - `ClosePendingLongOrders` / `ClosePendingShortOrders` – cancel pending buy or sell orders.
  - `SpecificOrderId` – only touch orders with the given transaction id when non-zero.
  - `CloseOrdersWithinRange`, `CloseRangeHigh`, `CloseRangeLow` – limit by entry price range.
  - `EnableTimeControl`, `StartCloseTime`, `StopCloseTime` – apply only during a specific time window.
- **Default Values**:
  - All closing options enabled.
  - `SpecificOrderId` = 0.
  - `CloseOrdersWithinRange` = false.
  - `CloseRangeHigh` = 0.
  - `CloseRangeLow` = 0.
  - `EnableTimeControl` = false.
  - `StartCloseTime` = 02:00.
  - `StopCloseTime` = 02:30.
- **Notes**:
  - The strategy does not open new positions.
  - Price range filters are ignored when bounds are zero or negative.
  - When `CloseAllSecurities` is enabled, positions across the entire portfolio are processed.
