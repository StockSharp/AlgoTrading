# Triple Stochastic MTF Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy runs three Stochastic Oscillators on different timeframes and trades when the smallest timeframe crosses its signal line in the direction confirmed by the higher timeframes. It is designed to capture short-term reversals within a larger trend context.

The primary timeframe (default 30-minute) and secondary timeframe (default 15-minute) determine the market bias. The entry timeframe (default 5-minute) waits for a %K and %D crossover opposite to the previous bar, signaling a pullback. Positions are closed whenever any of the monitored timeframes signals a trend change against the active trade.

## Details

- **Entry Criteria**:
  - **Long**: Previous %K > %D on the 5-minute chart, current %K ≤ %D, and both higher timeframes show %K > %D.
  - **Short**: Previous %K < %D on the 5-minute chart, current %K ≥ %D, and both higher timeframes show %K < %D.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Any timeframe switches to downtrend (%K < %D).
  - **Short**: Any timeframe switches to uptrend (%K > %D).
- **Stops**: Not implemented by default.
- **Default Values**:
  - `Timeframe 1` = 30-minute.
  - `Timeframe 2` = 15-minute.
  - `Timeframe 3` = 5-minute.
  - `%K Period` = 5.
  - `%D Period` = 3.
  - `Slowing` = 3.
- **Filters**:
  - Category: Trend following / Pullback
  - Direction: Both
  - Indicators: Stochastic Oscillator
  - Stops: No
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
