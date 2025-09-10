# Contrarian DC Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Contrarian DC strategy fades Donchian Channel breakouts. It buys when price pierces the lower band and sells when price touches the upper band. After a stop-loss, entries in the same direction are paused for a number of candles. Risk management uses symmetric stop-loss and take-profit based on a risk/reward ratio.

## Details
- **Entry Criteria**:
  - **Long**: Price low <= Donchian Low && pause satisfied
  - **Short**: Price high >= Donchian High && pause satisfied
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Stop**: Percentage stop-loss
  - **Target**: Risk/Reward based take-profit
  - **Band**: Close when reaching opposite band
- **Stops**: Yes, percentage-based
- **Default Values**:
  - `DonchianPeriod` = 20
  - `RiskRewardRatio` = 1.7m
  - `StopLossPercent` = 0.3m
  - `PauseCandles` = 3
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Donchian Channel
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

