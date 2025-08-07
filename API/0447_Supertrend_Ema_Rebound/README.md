# SuperTrend + EMA Rebound Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The system trades in the direction of SuperTrend and looks for pullbacks to an
exponential moving average. A position is opened either when the SuperTrend line
flips direction or when price rebounds from the EMA while remaining in the
prevailing SuperTrend bias. This combination attempts to capture the first leg
of a new move and subsequent retracements within an established trend.

A percentage based take profit can be enabled via the built‑in protection module
by setting the take profit type to "%". The defaults favor long trades but short
entries can also be activated. Because the strategy relies on direction changes,
it is most effective in trending markets where SuperTrend reacts quickly to
momentum shifts.

## Details

- **Entry Criteria**:
  - SuperTrend flips to uptrend, or price rebounds above EMA during uptrend.
  - SuperTrend flips to downtrend, or price rebounds below EMA during downtrend.
- **Long/Short**: Long enabled by default, short optional.
- **Exit Criteria**:
  - Opposite SuperTrend flip.
  - Optional take profit handled by protection module.
- **Stops**: Percentage take profit via protection; no stop loss included.
- **Default Values**:
  - ATR period = 10, ATR factor = 3.0.
  - EMA length = 20, TP = 1.5%.
- **Filters**:
  - Category: Trend following
  - Direction: Both (long default)
  - Indicators: SuperTrend, EMA
  - Stops: Optional TP
  - Complexity: Moderate
  - Timeframe: Short/medium
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
