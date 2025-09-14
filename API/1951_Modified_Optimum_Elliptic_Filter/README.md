# Modified Optimum Elliptic Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy applies the *Modified Optimum Elliptic Filter* indicator described by John F. Ehlers to detect directional turns. The indicator is a two–pole digital filter that smooths the average of high and low prices using the following recursive formula:

```
F(t) = 0.13785*(2*HL2(t) - HL2(t-1))
     + 0.0007 *(2*HL2(t-1) - HL2(t-2))
     + 0.13785*(2*HL2(t-2) - HL2(t-3))
     + 1.2103 * F(t-1) - 0.4867 * F(t-2)
```

Where `HL2` is the midpoint `(High + Low)/2` of each candle.

The strategy reads the last three filter values to determine momentum. If the indicator is rising and the most recent value exceeds the previous one, a long position is opened. If the indicator is falling and the current value is below the previous one, a short position is opened. Positions are reversed when the opposite condition occurs.

## Details

- **Entry Criteria**:
  - **Long**: `F(t-1) < F(t-2)` and `F(t) > F(t-1)`.
  - **Short**: `F(t-1) > F(t-2)` and `F(t) < F(t-1)`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Position is reversed on opposite signal.
- **Stops**: No explicit stops.
- **Default Values**:
  - `Candle Type` = 4-hour time frame.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Medium-term
