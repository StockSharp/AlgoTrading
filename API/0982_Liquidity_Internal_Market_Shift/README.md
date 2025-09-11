# Liquidity Internal Market Shift Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy detects internal market structure shifts that coincide with liquidity sweeps at recent highs or lows. A trade is opened when price touches a liquidity line and then shifts structure in the opposite direction. Trading can be limited to bullish or bearish setups only, or both.

## Details

- **Entry Criteria**:
  - **Long**: Price closes above prior bearish structure and has touched the recent low liquidity line.
  - **Short**: Price closes below prior bullish structure and has touched the recent high liquidity line.
- **Long/Short**: Both directions or selectable Bullish Only / Bearish Only.
- **Exit Criteria**:
  - Opposite signal after entry.
  - Stop-loss at `StopLossPips` pips.
  - Optional take-profit at `TakeProfitPips` pips.
- **Stops**: Yes, configurable stop-loss and optional take-profit.
- **Filters**:
  - Trades only within the specified time range.
  - Signal lock prevents repeated entries for several bars.
