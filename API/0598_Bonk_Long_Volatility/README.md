# BONK Long Volatility
[Русский](README_ru.md) | [中文](README_cn.md)

This long-only strategy enters on strong bullish conditions combining moving averages, volatility and volume filters. It buys when the market is trending up, volatility expands and momentum indicators confirm strength. Exits use fixed take-profit, stop-loss and an ATR-based trailing stop.

## Details

- **Entry Criteria**: Fast MA above slow MA, price range greater than ATR * `AtrMultiplier`, RSI between `RsiOversold` and `RsiOverbought`, MACD line above signal and zero, volume above SMA * `VolumeThreshold`, close above fast MA, candle within last `LookbackDays`.
- **Long/Short**: Long only.
- **Exit Criteria**: Take-profit, stop-loss or ATR trailing stop.
- **Stops**: Yes.
- **Default Values**:
  - `ProfitTargetPercent` = 5.0m
  - `StopLossPercent` = 3.0m
  - `AtrLength` = 10
  - `AtrMultiplier` = 1.5m
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeSmaLength` = 20
  - `VolumeThreshold` = 1.5m
  - `MaFastLength` = 5
  - `MaSlowLength` = 13
  - `LookbackDays` = 30
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: SMA, ATR, RSI, MACD, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

