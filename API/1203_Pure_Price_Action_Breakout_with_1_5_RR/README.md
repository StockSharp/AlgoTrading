# Pure Price Action Breakout with 1:5 RR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Pure Price Action Breakout with 1:5 RR strategy uses a crossover of two EMAs confirmed by RSI and volume. The stop loss is based on ATR and the take profit is five times the risk.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA crosses above slow EMA, RSI > 50, volume above 20‑period SMA.
  - **Short**: Fast EMA crosses below slow EMA, RSI < 50, volume above 20‑period SMA.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - ATR-based stop loss and 1:5 risk-reward take profit.
- **Stops**: Stop loss = 1.5 × ATR, take profit = 5 × risk.
- **Default Values**:
  - `FastPeriod` = 9
  - `SlowPeriod` = 21
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `VolumePeriod` = 20
  - `StopLossFactor` = 1.5
  - `RiskRewardRatio` = 5
  - `MaxTradesPerDay` = 5
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: EMA, RSI, ATR, Volume SMA
  - Stops: ATR stop loss, 1:5 take profit
  - Complexity: Low
  - Timeframe: 5m or 15m
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
