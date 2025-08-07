# Bollinger Breakout
[Русский](README_ru.md) | [中文](README_cn.md)

Bollinger Breakout seeks to capture moves that push beyond the Bollinger Bands
and keep going. When price closes above the upper band or below the lower band,
the strategy enters in the direction of the breakout if optional confirmations
support the trade.

RSI, Aroon and moving‑average filters can be enabled to validate momentum and
trend. An optional stop‑loss helps control risk. Positions are closed when price
reaches the opposite band or the stop is triggered.

This approach favors markets prone to strong trends where band breaks lead to
follow‑through rather than mean reversion.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Close above upper band and all enabled filters confirm.
  - **Short**: Close below lower band and all enabled filters confirm.
- **Exit Criteria**: Touch of opposite band or stop‑loss if `UseSL`.
- **Stops**: Optional stop‑loss (`UseSL`).
- **Default Values**:
  - `UseRSI` = True
  - `UseAroon` = False
  - `UseMA` = True
  - `UseSL` = True
- **Filters**:
  - Category: Breakout
  - Direction: Long & Short
  - Indicators: Bollinger Bands, RSI, Aroon, Moving Average
  - Complexity: Moderate
  - Risk level: High
