# VWAP RSI Scalper FINAL v1
[Русский](README_ru.md) | [中文](README_cn.md)

Scalping strategy combining VWAP and RSI with ATR-based exits and daily trade limits.

## Details

- **Entry Criteria**: Price relative to VWAP and EMA with RSI thresholds within session.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR-based stop and target.
- **Stops**: Yes.
- **Default Values**:
  - `RsiLength` = 3
  - `RsiOversold` = 35m
  - `RsiOverbought` = 70m
  - `EmaLength` = 50
  - `SessionStart` = 09:00
  - `SessionEnd` = 16:00
  - `MaxTradesPerDay` = 3
  - `AtrLength` = 14
  - `StopAtrMult` = 1m
  - `TargetAtrMult` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Scalping
  - Direction: Both
  - Indicators: VWAP, RSI, EMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
