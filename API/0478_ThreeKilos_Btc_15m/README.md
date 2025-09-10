# Three Kilos BTC 15m Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Three Kilos BTC 15m strategy combines three Triple Exponential Moving Averages (TEMA) with a Supertrend filter. A long position is opened when the middle TEMA crosses above the short TEMA, stays above the slow TEMA, and the Supertrend indicates an uptrend. A short position is opened when the short TEMA crosses above the middle TEMA, remains below the slow TEMA, and the Supertrend shows a downtrend. Fixed percentage take profit and stop loss manage risk.

## Details

- **Entry Criteria**:
  - **Long**: TEMA2 crosses above TEMA1, TEMA2 > TEMA3, Supertrend uptrend.
  - **Short**: TEMA1 crosses above TEMA2, TEMA2 < TEMA3, Supertrend downtrend.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Take profit or stop loss.
- **Stops**: 1% take profit and 1% stop loss.
- **Default Values**:
  - `ShortPeriod` = 30
  - `LongPeriod` = 50
  - `Long2Period` = 140
  - `AtrLength` = 10
  - `Multiplier` = 2
  - `TakeProfit` = 1%
  - `StopLoss` = 1%
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: TEMA, Supertrend, ATR
  - Stops: Take profit and stop loss
  - Complexity: Medium
  - Timeframe: 15m
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
