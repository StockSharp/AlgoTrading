# Trend Magic with EMA, SMA, and Auto-Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses a CCI-based Trend Magic line together with EMA(45), SMA(90), and SMA(180) filters. A long trade opens when Trend Magic switches to blue during a bullish moving average alignment. Short trades occur when the line turns red and moving averages align bearishly. Each position has a stop at SMA90 and a take profit based on a fixed risk/reward ratio.

## Details

- **Entry Criteria**:
  - **Long**: `EMA45 > SMA90 > SMA180` and Trend Magic turns blue.
  - **Short**: `EMA45 < SMA90 < SMA180` and Trend Magic turns red.
- **Exits**: Stop-loss at SMA90 captured on entry and take-profit at `entry ± risk * ratio`.
- **Stops**: Both stop-loss and take-profit.
- **Default Values**:
  - `CCI Period` = 21
  - `ATR Period` = 7
  - `ATR Multiplier` = 1.0
  - `Risk Reward` = 1.5
