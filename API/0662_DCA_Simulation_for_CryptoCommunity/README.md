# DCA Simulation for CryptoCommunity Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy simulates dollar cost averaging with optional safety orders and a trailing take-profit. It starts with a base order and can periodically invest additional capital or average down after price drops.

## Details

- **Entry Criteria**:
  - When no position is open and the date is within the configured range, buy a base amount.
  - Optional periodic DCA orders every N candles.
  - Optional safety orders when price falls by a specified percentage from the recent high.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Take profit at a target percentage, optionally with trailing stop.
- **Stops**: Take profit / trailing stop.
- **Default Values**:
  - Base order = 100 USD.
  - DCA amount = 10 USD every 30 candles.
  - Safety order amount = 100 USD with 15% price deviation.
  - Take profit = 1000%, trailing = 25%.
  - Start date = 2021-11-01, end date = 9999-01-01.
- **Filters**:
  - Category: Accumulation.
  - Direction: Long.
  - Indicators: None.
  - Stops: Yes.
  - Complexity: Medium.
  - Timeframe: Any.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Medium.
