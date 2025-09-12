# Long Explosive V1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Long Explosive V1 enters a long position when the close price jumps by a defined percentage relative to the previous bar. The position is closed when price drops by the configured percentage or before opening a new long trade.

## Details

- **Entry Criteria**:
  - **Long**: `Close - PrevClose > Close * Price increase (%) / 100`.
- **Long/Short**: Long only.
- **Exit Criteria**: `Close - PrevClose < -Close * Price decrease (%) / 100` or before a new long entry.
- **Stops**: None.
- **Default Values**:
  - `Price increase (%)` = 1
  - `Price decrease (%)` = 1
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: Price
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
