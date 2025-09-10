# Bollinger Bands & Fibonacci Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades Bollinger Band breakouts filtered by Fibonacci levels. A long position opens when price crosses above the upper band and the candle's low is above a Fibonacci-based support. A short position opens when price crosses below the lower band and the candle's high is below a Fibonacci-based resistance.

## Details

- **Entry Criteria**:
  - **Long**: Close crosses above upper band and low > Fibonacci low.
  - **Short**: Close crosses below lower band and high < Fibonacci high.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Close crosses below middle band.
  - **Short**: Close crosses above middle band.
- **Stops**: None.
- **Default Values**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2
  - `FibonacciLength` = 50
  - `FibonacciLevel0` = 0
  - `FibonacciLevel100` = 1
- **Filters**:
  - Category: Band breakout
  - Direction: Both
  - Indicators: Bollinger Bands, Highest, Lowest
  - Stops: None
  - Complexity: Low
  - Timeframe: 1H
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
