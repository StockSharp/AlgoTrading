# Multi Indicator Trend Following Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA crossover strategy with RSI and volume confirmation. Uses ATR-based stop loss and take profit.

## Details

- **Entry Criteria**: Fast EMA crosses above/below slow EMA with RSI filter and high volume
- **Long/Short**: Both
- **Exit Criteria**: ATR-based stop loss and take profit
- **Stops**: Yes, ATR-based
- **Default Values**:
  - `CandleType` = 5 minute
  - `FastMaLength` = 10
  - `SlowMaLength` = 30
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `AtrPeriod` = 14
  - `StopLossAtrMultiplier` = 2
  - `TakeProfitAtrMultiplier` = 3
- **Filters**:
  - Category: Trend-Following
  - Direction: Both
  - Indicators: EMA, RSI, ATR, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
