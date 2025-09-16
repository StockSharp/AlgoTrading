# RAVI Histogram Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy converts the MetaTrader RAVI Histogram expert to StockSharp. It measures trend strength as the percentage difference between a fast and a slow EMA. The result is compared with upper and lower levels to decide when to trade.

When the RAVI value rises above the upper level the market is considered bullish. Short positions are closed and, if enabled, a long position is opened. When the value falls below the lower level the strategy closes longs and may open a short. By default it operates on four‑hour candles.

## Details

- **Entry Criteria**:
  - **Long**: RAVI crosses upward through `UpLevel`.
  - **Short**: RAVI crosses downward through `DownLevel`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite RAVI signal closes existing positions.
- **Stops**: None.
- **Filters**: None.
- **Timeframe**: 4‑hour candles by default.
- **Parameters**:
  - `FastLength` and `SlowLength` – EMA periods for RAVI calculation.
  - `UpLevel` and `DownLevel` – thresholds defining trending zones.
  - `BuyOpen`, `SellOpen`, `BuyClose`, `SellClose` – enable or disable operations in each direction.
