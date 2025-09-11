# Donchian Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A breakout system using Donchian Channels with volatility and volume filters.

The strategy buys when price closes above the upper Donchian channel and the trend is confirmed by an EMA and RSI reading above 50. Shorts are taken on breaks below the lower channel. Positions are closed on an opposite Donchian signal or when an ATR-based stop is hit.

## Details

- **Entry Criteria**: Donchian channel breakout with EMA, RSI, volatility and volume filters.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite breakout or ATR stop.
- **Stops**: ATR-based.
- **Default Values**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `EmaLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Donchian, ATR, EMA, RSI, Volume
  - Stops: ATR Stop
  - Complexity: Intermediate
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
