# Volatility Pulse with Dynamic Exit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Momentum-based strategy detecting volatility expansion. Enters in the direction of momentum when ATR rises above its average and exits using ATR-based stop and take-profit after a holding period.

## Details

- **Entry Criteria**: ATR volatility expansion with momentum confirmation
- **Long/Short**: Both
- **Exit Criteria**: Stop loss and take profit placed after holding period
- **Stops**: ATR-based stop, risk-reward take profit
- **Default Values**:
  - `AtrLength` = 14
  - `MomentumLength` = 20
  - `VolThreshold` = 0.5
  - `MinVolatility` = 1.0
  - `ExitBars` = 42
  - `RiskReward` = 2
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: ATR, SMA, Momentum
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
