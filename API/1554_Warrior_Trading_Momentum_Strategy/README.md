# Warrior Trading Momentum Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Momentum strategy inspired by Warrior Trading combining gap detection, VWAP and red-to-green setups.

## Details

- **Entry Criteria**: Gap-and-go, red-to-green, or VWAP bounce with volume spike.
- **Long/Short**: Long only.
- **Exit Criteria**: ATR-based stop, take profit and trailing.
- **Stops**: Yes.
- **Default Values**:
  - `GapThreshold` = 2m
  - `GapVolumeMultiplier` = 2m
  - `VwapDistance` = 0.5m
  - `MinRedCandles` = 3
  - `RiskRewardRatio` = 2m
  - `TrailingStopTrigger` = 1m
  - `MaxDailyTrades` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: VWAP, RSI, EMA, ATR, Volume
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
