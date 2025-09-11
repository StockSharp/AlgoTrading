# REIT ETF Trading System Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Weekly REIT ETF strategy combining Bollinger Bands breakouts, Donchian channel trend signals and filters based on Treasury yields and correlation with SPY and TNX.

## Details

- **Entry Criteria**:
  - Bollinger Bands breakout with yield and correlation filters.
  - Donchian channel breakout with yield or correlation or TNX trend conditions.
- **Long/Short**: Long.
- **Exit Criteria**:
  - TNX-based trailing stop.
  - Overbought and stop conditions.
- **Stops**: ATR trailing and percentage loss.
- **Default Values**:
  - `BollingerLength` = 15
  - `BollingerMultiplier` = 2
  - `TnxLookbackPeriod` = 25
  - `TnxMinChangePercent` = 15
  - `DonchianChannelLength` = 30
  - `MaxCorrelationForBuy` = 0.3
  - `MinYield` = 2
  - `AtrStopMultiplier` = 1.5
  - `StopLossPercent` = 8
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: Bollinger Bands, Donchian Channel, ATR, Stochastic, Correlation
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Weekly
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
