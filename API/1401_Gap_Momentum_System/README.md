# Gap Momentum System Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implements the gap momentum system from Perry Kaufman. The strategy compares accumulated up and down gaps and trades when the signal rises or falls.

## Details
- **Entry Criteria**: Rising signal -> buy, falling signal -> sell or reverse.
- **Long/Short**: Configurable.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `Period` = 40
  - `SignalPeriod` = 20
  - `LongOnly` = true
- **Filters**:
  - Category: Momentum
  - Direction: Both or long only
  - Indicators: Gap momentum
  - Stops: No
  - Complexity: Low
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
