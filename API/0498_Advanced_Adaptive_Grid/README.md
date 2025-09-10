# Advanced Adaptive Grid Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Advanced Adaptive Grid Strategy uses multiple technical indicators to evaluate trend direction and builds a dynamic grid of entry levels. The grid size adapts to volatility via ATR and orders are placed when price touches grid levels in the trend direction. Risk controls include fixed stop-loss, take-profit, trailing stop, time-based exit and daily loss limit.

## Details

- **Entry Criteria**:
  - In trending markets price reaching calculated grid levels with RSI confirmation.
  - In sideways markets overbought/oversold RSI triggers grid entries.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Stop-loss, take-profit, trailing stop, trend reversal or time-based exit.
- **Stops**: Fixed and trailing.
- **Default Values**:
  - `BaseGridSize` = 1
  - `MaxPositions` = 5
  - `UseVolatilityGrid` = True
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `ShortMaLength` = 20
  - `LongMaLength` = 50
  - `SuperLongMaLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `UseTrailingStop` = True
  - `TrailingStopPercent` = 1
  - `MaxLossPerDay` = 5
  - `TimeBasedExit` = True
  - `MaxHoldingPeriod` = 48
- **Filters**:
  - Category: Grid / Trend
  - Direction: Both
  - Indicators: ATR, SMA, MACD, RSI, Momentum
  - Stops: Yes
  - Complexity: High
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
