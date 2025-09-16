# EMA Sticker Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses an Exponential Moving Average (EMA) to follow short‑term trends. A long position is opened when the close price crosses above the EMA, while a short position is opened when the close price crosses below the EMA. Optional fixed stop‑loss and take‑profit levels help manage risk.

## Details

- **Entry Criteria**:
  - **Long**: `Close > EMA`.
  - **Short**: `Close < EMA`.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal or configured stop levels reached.
- **Stops**: Yes, optional stop‑loss and take‑profit in price units.
- **Default Values**:
  - `MA period` = 5.
  - `Stop loss` = 0.001.
  - `Take profit` = 0.001.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Short-term
