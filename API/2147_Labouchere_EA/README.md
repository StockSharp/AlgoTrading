# Labouchere EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a Stochastic Oscillator crossover with a Labouchere money management sequence. The Stochastic indicator generates signals when %K crosses %D. The Labouchere system adjusts trade volume after each closed position: losses append a new element equal to the sum of the first and last numbers in the sequence, while profits remove these elements.

Trades are taken only on finished candles. The sequence can optionally restart when all numbers are removed. A time filter allows trading within a specific intraday window, and opposite signals can close existing positions. Fixed stop-loss and take-profit levels (in price steps) are supported.

## Details
- **Entry Criteria**:
  - **Long**: %K crosses above %D.
  - **Short**: %K crosses below %D.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Optional opposite signal exit.
  - Fixed stop-loss and take-profit (if set).
- **Stops**: Yes.
- **Money Management**: Labouchere sequence.
- **Default Values**:
  - `LotSequence` = "0.01,0.02,0.01,0.02,0.01,0.01,0.01,0.01"
  - `NewRecycle` = true
  - `StopLoss` = 40
  - `TakeProfit` = 50
  - `IsReversed` = false
  - `UseOppositeExit` = false
  - `UseWorkTime` = false
  - `StartTime` = 00:00
  - `StopTime` = 24:00
  - `KPeriod` = 10
  - `DPeriod` = 190
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
