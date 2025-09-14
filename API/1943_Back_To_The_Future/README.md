# Back to the Future Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This momentum strategy compares the current closing price with the price from a specified number of minutes ago. When the price advances beyond a defined threshold relative to the historical price, the system opens a long position. Conversely, when the price falls below the negative threshold, it opens a short position. The approach assumes that strong moves away from the past price indicate emerging trends.

The strategy operates on completed candles and works on any instrument and timeframe supported by StockSharp. Built‑in take‑profit and stop‑loss levels manage risk once a position is opened. A queue of past prices maintains a rolling history to evaluate the price difference.

## Details

- **Entry Criteria**:
  - **Long**: `Close(t) - Close(t-Δ) > BarSize`.
  - **Short**: `Close(t) - Close(t-Δ) < -BarSize`.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: `Close >= Entry + TakeProfit` or `Close <= Entry - StopLoss`.
  - **Short**: `Close <= Entry - TakeProfit` or `Close >= Entry + StopLoss`.
- **Stops**: Yes, fixed take‑profit and stop‑loss in price units.
- **Default Values**:
  - `BarSize = 0.25`
  - `HistoryMinutes = 60`
  - `TakeProfit = 10`
  - `StopLoss = 5000`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Short‑term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
