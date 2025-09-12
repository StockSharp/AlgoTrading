# Moving Average Crossover Swing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades when a fast exponential moving average crosses a medium one with optional confirmation from a slow MA and MACD histogram. Uses ATR-based stop loss and take profit and can exit on a secondary MA cross.

## Details

- **Entry Criteria**:
  - Fast EMA crosses above medium EMA for long, below for short.
  - Optional: price above/below slow EMA.
  - Optional: MACD histogram above/below zero.
- **Long/Short**: Configurable.
- **Exit Criteria**: ATR-based stop loss and take profit or optional exit MA cross.
- **Stops**: Yes, ATR multiples.
- **Default Values**:
  - `FastPeriod` = 5
  - `MediumPeriod` = 10
  - `SlowPeriod` = 50
  - `FastExitPeriod` = 5
  - `MediumExitPeriod` = 10
  - `AtrPeriod` = 14
  - `AtrStopMultiplier` = 1.4
  - `AtrTakeMultiplier` = 3.2
  - `EnableSlow` = true
  - `EnableMacd` = true
  - `EnableLong` = true
  - `EnableShort` = false
  - `EnableCrossExit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend following
  - Direction: Configurable
  - Indicators: EMA, MACD, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: 1m (default)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
