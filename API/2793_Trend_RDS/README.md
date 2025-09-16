# Trend RDS
[Русский](README_ru.md) | [中文](README_cn.md)

Trend RDS searches for clear directional sequences in price action. When three completed candles form strictly higher lows it treats the structure as a bullish trend leg. Three strictly lower highs mark a bearish setup. A protection rule blocks entries when the same three bars simultaneously create both higher lows and lower highs, which usually indicates a contracting triangle instead of a directional move. The strategy can optionally invert the direction through the `Reverse` parameter.

Trading is limited to a configurable time window (default 09:00–12:00). When the window is open and a valid pattern appears, the strategy closes any opposite exposure, opens a new market position at the candle close, and places stop-loss and take-profit orders measured in pips. The pip distance is derived from the instrument's price step, mirroring the original MetaTrader logic. An optional trailing stop moves the protective stop forward once price advances by the trailing distance plus the trailing step. Trailing adjustments are evaluated only while the session window is active.

Position size is recalculated on every entry. The strategy allocates a fraction of portfolio equity defined by `RiskPercent` and divides it by the monetary risk represented by the chosen stop distance. This produces dynamic sizing that scales with both account size and stop width while respecting the minimum `Volume` value. Setting any risk-related parameter to zero disables that feature, allowing fixed-size or unprotected entries when desired.

## Details
- **Entry Criteria**: Three consecutive candles with higher lows trigger longs (or shorts when `Reverse` is true). Three consecutive lower highs trigger shorts (or longs in reverse mode). Signals are ignored if the same three bars also satisfy both conditions simultaneously.
- **Long/Short**: Both directions with an optional reversal switch.
- **Exit Criteria**: Market exits when the tracked stop-loss, take-profit, or trailing stop levels are breached.
- **Stops**: Fixed stop-loss and take-profit in pips with an incremental trailing stop (requires both trailing parameters to be positive).
- **Time Window**: Trades only between `StartTime` and `EndTime` (defaults 09:00–12:00 exchange time).
- **Position Sizing**: Risk-based sizing using `RiskPercent` of portfolio equity relative to the current stop distance (falls back to `Volume` if sizing cannot be calculated).
- **Default Values**:
  - `StopLossPips` = 30
  - `TakeProfitPips` = 65
  - `TrailingStopPips` = 0
  - `TrailingStepPips` = 5
  - `RiskPercent` = 3
  - `StartTime` = 09:00
  - `EndTime` = 12:00
  - `Reverse` = false
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Price action (highs/lows)
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
