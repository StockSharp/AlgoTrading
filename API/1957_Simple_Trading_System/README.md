# Simple Trading System Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the Simple Trading System from MetaTrader. It uses a moving average shifted by several bars and compares the current close with prior closes to detect short-term trend reversals. A buy signal occurs when the moving average is below its value `MaShift` bars ago and the close is between the closes `MaShift` and `MaPeriod + MaShift` bars ago while the candle is bearish. A sell signal is the mirror opposite. Depending on parameters, the strategy can open and/or close long or short positions when signals appear. Optional stop-loss and take-profit levels can be configured.

## Details

- **Entry Criteria:**
  - **Long**: `MA(t) <= MA(t+MaShift)` && `Close(t) >= Close(t+MaShift)` && `Close(t) <= Close(t+MaPeriod+MaShift)` && `Close(t) < Open(t)`
  - **Short**: `MA(t) >= MA(t+MaShift)` && `Close(t) <= Close(t+MaShift)` && `Close(t) >= Close(t+MaPeriod+MaShift)` && `Close(t) > Open(t)`
- **Long/Short**: Both sides depending on `BuyPositionOpen` and `SellPositionOpen`.
- **Exit Criteria**: Opposite signal triggers closing if `BuyPositionClose` or `SellPositionClose` is enabled.
- **Stops**: Optional. `StopLoss` and `TakeProfit` in absolute price units via `StartProtection`.
- **Default Values:**
  - `MaType` = EMA
  - `MaPeriod` = 2
  - `MaShift` = 4
  - `PriceType` = Close
  - `CandleType` = 6-hour candles
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `Volume` = 1
- **Filters:**
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving Average
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
