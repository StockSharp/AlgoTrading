# Bias And Sentiment Strength Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy aggregates multiple momentum and volume indicators (MACD, RSI, Stochastic, Awesome Oscillator, Alligator averages and volume bias) into a single bias value. A long position is opened when the combined bias is above zero and a short position when it is below zero.

## Details

- **Entry Criteria**:
  - **Long**: Combined bias > 0.
  - **Short**: Combined bias < 0.
- **Long/Short**: Both.
- **Exit Criteria**: Reverse signal.
- **Stops**: Stop-loss percentage via `StopLossPercent`.
- **Default Values**:
  - MACD fast 12, slow 26, signal 9.
  - RSI period 14.
  - Stochastic periods 21/14/14.
  - Awesome Oscillator periods 5/34.
  - Volume bias length 30.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Complex
  - Timeframe: Medium-term
