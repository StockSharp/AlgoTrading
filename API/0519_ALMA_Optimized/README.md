# ALMA Optimized Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy blends an Arnaud Legoux Moving Average with a long-term EMA, ADX, RSI, and Bollinger Bands. An ATR based filter ensures sufficient volatility. Positions use ATR multiples for stop-loss and take-profit, with an optional time-based exit.

## Details

- **Entry Criteria**:
  - **Long**: ATR above threshold, close above EMA and ALMA, RSI > 30, ADX > 30, close below upper Bollinger band, and cooldown passed.
  - **Short**: Close crosses below fast EMA under same volatility filter.
- **Exit Criteria**:
  - Stop-loss or take-profit based on ATR multiples.
  - Optional time-based exit in bars.
- **Default Values**:
  - Fast EMA = 20.
  - ATR length = 14.
  - EMA length = 72.
  - ADX length = 10.
  - RSI length = 14.
  - Cooldown = 7 bars.
  - Bollinger multiplier = 3.0.
  - Stop ATR multiplier = 5.0.
  - Take ATR multiplier = 4.0.
  - Time exit = 0.
  - Minimum ATR = 0.005.
- **Filters**:
  - Category: Trend + Momentum
  - Direction: Both
  - Indicators: EMA, ALMA, ADX, RSI, ATR, Bollinger Bands
  - Stops: ATR based
  - Complexity: Moderate
  - Timeframe: Short/medium
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
