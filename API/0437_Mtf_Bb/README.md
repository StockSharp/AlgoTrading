# Multi-Timeframe Bollinger Bands Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Applies Bollinger Bands on both a primary and a higher timeframe. Trades when price pierces the higher timeframe bands and optionally filters entries with a long-term moving average. The goal is to fade extremes against the broader trend.

The strategy supports both long and short positions. A stop-loss percentage can be enabled for risk management. Using multiple timeframes helps to avoid trades against dominant market structure.

## Details

- **Entry Criteria**:
  - **Long**: Close below the higher timeframe lower band and above the MA filter (if enabled).
  - **Short**: Close above the higher timeframe upper band and below the MA filter (if enabled).
- **Exit Criteria**:
  - Long: Price closes above the current timeframe upper band.
  - Short: Price closes below the current timeframe lower band.
- **Indicators**:
  - Bollinger Bands on two timeframes (length 20, multiplier 2)
  - Optional EMA filter (period 200)
- **Stops**: Optional stop-loss via StartProtection (% based).
- **Default Values**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `UseMaFilter` = False
  - `MaLength` = 200
  - `SLPercent` = 2
- **Filters**:
  - Counter-trend with MTF context
  - Timeframe: main 5m, MTF 60m by default
  - Indicators: Bollinger Bands, EMA
  - Stops: optional
  - Complexity: Moderate
