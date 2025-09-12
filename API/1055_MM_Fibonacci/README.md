# MM Fibonacci Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy calculates Murrey Math Fibonacci levels and trades breakouts. It buys when price breaks above the 100% level in an upward context and sells when price drops below the 0% level in a downward context. Positions are closed when price crosses the 50% level against the trade.

## Details

- **Entry Criteria**:
  - **Long**: Price closes above the 100% level while the most recent extreme was a high.
  - **Short**: Price closes below the 0% level while the most recent extreme was a low.
- **Exit Criteria**:
  - **Long**: Price falls below the 50% level.
  - **Short**: Price rises above the 50% level.
- **Indicators**: Highest, Lowest.
- **Long/Short**: Both.
- **Stops**: No.
