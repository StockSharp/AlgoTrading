# Bot for Spot Market - Custom Grid Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Bot for Spot Market - Custom Grid strategy buys an initial position and adds new orders when price drops by a specified percentage below the last entry. It closes all positions when price rises above the average entry price by the profit target.

## Details

- **Entry Criteria**:
  - Buy at start time.
  - Buy additional quantity when price drops `NextEntryPercent`% below the last entry.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Close all positions when price exceeds the average entry price by `ProfitPercent`% and the open position is profitable.
- **Stops**: None.
- **Default Values**:
  - `OrderValue` = 10
  - `MinAmountMovement` = 0.00001
  - `Rounding` = 5
  - `NextEntryPercent` = 0.5
  - `ProfitPercent` = 2
- **Filters**:
  - Category: Grid trading
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
