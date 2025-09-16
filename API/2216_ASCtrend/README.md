# ASCtrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Williams %R indicator to detect rapid reversals similar to the ASCtrend approach. It sells when the indicator rises from an oversold level to an overbought level and buys when the opposite occurs.

## Details

- **Entry Criteria**:
  - Sell when Williams %R crosses from oversold (below `x2`) to overbought (above `x1`).
  - Buy when Williams %R crosses from overbought (above `x1`) to oversold (below `x2`).
- **Long/Short**: Both.
- **Exit Criteria**:
  - Reverse signal closes and flips the position.
- **Stops**: No.
- **Default Values**:
  - `Risk` = 4
  - `CandleType` = 1 hour
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Williams %R
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
