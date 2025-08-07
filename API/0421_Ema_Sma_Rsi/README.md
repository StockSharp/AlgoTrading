# EMA/SMA + RSI Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy tracks three exponential moving averages (fast, medium, and slow) along with an RSI filter to participate in emerging trends. A trade is triggered when the fast average crosses the medium one in the direction of the prevailing slow average, indicating that momentum is accelerating. Only candles that close in the direction of the crossover are considered to avoid whipsaws.

A protective exit can optionally close positions after a user-defined number of bars if they remain profitable. The RSI acts as an overbought/oversold guard to exit when momentum becomes stretched.

Backtests show the technique works best on liquid crypto pairs during trending phases where moving averages offer clear separation.

## Details

- **Entry Criteria**:
  - **Long**: `EMA_fast > EMA_medium` and `EMA_fast(t-1) <= EMA_medium(t-1)` and `Close > EMA_slow` and `Close > Open`
  - **Short**: `EMA_fast < EMA_medium` and `EMA_fast(t-1) >= EMA_medium(t-1)` and `Close < EMA_slow` and `Close < Open`
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: `RSI > 70` or `X bars in profit and Close > entry`
  - **Short**: `RSI < 30` or `X bars in profit and Close < entry`
- **Stops**: None.
- **Default Values**:
  - `EMA_fast` = 10
  - `EMA_medium` = 20
  - `EMA_slow` = 100
  - `RSI_length` = 14
  - `X bars` = 24
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, RSI
  - Stops: Optional time-based
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
