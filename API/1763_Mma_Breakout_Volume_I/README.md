# MMA Breakout Volume I Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts when the close price crosses a long-term Smoothed Moving Average (SMMA).
A long position is opened when the price rises above the SMMA and a short position is opened when it falls below.
Positions are exited when the price moves against the trade and crosses an Exponential Moving Average (EMA).

## Details

- **Entry Criteria**:
  - **Long**: Close price crosses above SMMA(200).
  - **Short**: Close price crosses below SMMA(200).
- **Exit Criteria**:
  - **Long**: Close price falls below EMA(5).
  - **Short**: Close price rises above EMA(5).
- **Long/Short**: Both.
- **Stops**: No fixed stop-loss, exit is driven by the EMA signal.
- **Default Values**:
  - `SMMA period` = 200
  - `EMA period` = 5
  - `Candle type` = 5-minute candles
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving Averages
  - Stops: No
  - Complexity: Simple
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
