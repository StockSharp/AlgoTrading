# Buy Sell Renko Based Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades Renko bricks created with an ATR based size. A long position is opened when the Renko close crosses above its open. A short position is opened when the close crosses below the open.

## Details

- **Entry Criteria**:
  - **Long**: Close crosses above open.
  - **Short**: Close crosses below open.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - ATR length 10.
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Renko
  - Stops: No
  - Complexity: Simple
  - Timeframe: Not time based
