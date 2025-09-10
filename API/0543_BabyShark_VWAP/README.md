# BabyShark VWAP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a volume-weighted average price (VWAP) band with an OBV-based RSI filter. Long trades occur when price drops below the lower deviation band and the RSI signals oversold. Short trades trigger when price rises above the upper band and RSI is overbought.

Stops use a small percentage loss and positions wait for a cooldown period before re-entry.

## Details

- **Entry Criteria**: Price crosses deviation bands with RSI confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Return to VWAP or stop loss.
- **Stops**: Yes.
- **Default Values**:
  - `Length` = 60
  - `RsiLength` = 5
  - `HigherLevel` = 70
  - `LowerLevel` = 30
  - `Cooldown` = 10
  - `StopLossPercent` = 0.6m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: VWAP, RSI, OBV
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
