[Русский](README_ru.md) | [中文](README_cn.md)

The strategy replicates the Firebird v0.60 envelope reversion expert. It measures a simple moving average and offsets it by a percentage to form upper and lower envelopes. When price pierces the upper band the strategy sells, and when the lower band breaks it buys. Additional positions are averaged in only if price moves at least one configurable pip step beyond the previous entry. The total stop loss is shared among all entries, preventing runaway trends from repeatedly re-entering in the same direction.

## Details

- **Entry Criteria**:
  - Calculate an SMA on either candle opens or the high/low midpoint.
  - Upper envelope = SMA × (1 + Percent/100); lower envelope = SMA × (1 − Percent/100).
  - Enter short on a close above the upper band (unless a recent stop locked shorts), enter long on a close below the lower band (unless longs are locked).
  - Average-in trades are allowed once price moves `PipStep` pips (optionally scaled by power) beyond the latest fill.
- **Long/Short**: Long and short.
- **Exit Criteria**:
  - Shared take profit at the averaged entry price ± `TakeProfit` pips.
  - Shared stop loss at the averaged entry price ∓ `StopLoss / position count` pips.
  - Blocking flag prevents re-entry in the same direction until an opposite signal triggers after a stop.
- **Stops**: Yes, aggregated stop loss and take profit.
- **Default Values**:
  - `MaLength` = 10
  - `Percent` = 0.3
  - `TradeOnFriday` = true
  - `UseHighLow` = false (use opens)
  - `PipStep` = 30
  - `IncreasementPower` = 0
  - `TakeProfit` = 30
  - `StopLoss` = 200
  - `TradeVolume` = 1
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: SMA envelopes
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: Optional Friday filter
  - Neural networks: No
  - Divergence: No
  - Risk level: High due to averaging
