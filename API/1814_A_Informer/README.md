# A Informer
[Русский](README_ru.md) | [中文](README_cn.md)

A Informer is a utility strategy that automatically attaches stop-loss and take-profit orders to existing positions and reports current profit in both USD and points. It does not generate trading signals and is intended to manage manually opened trades.

The strategy monitors new trades, converts realized profit to points using the instrument's price step, and logs the information for the trader. Protection is applied once the strategy starts.

## Details

- **Entry Criteria**: Manual trade entry; the strategy does not open positions.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss or take profit triggered.
- **Stops**: Yes (via `StartProtection`).
- **Default Values**:
  - `StopLoss` = 300 points
  - `TakeProfit` = 1000 points
- **Filters**:
  - Category: Utility
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low

