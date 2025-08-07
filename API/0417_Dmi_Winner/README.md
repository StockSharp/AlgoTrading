# DMI Winner
[Русский](README_ru.md) | [中文](README_cn.md)

DMI Winner is a trend‑following strategy based on the Directional Movement Index
(DMI). It opens trades when the `+DI` and `-DI` lines cross and the Average
Directional Index (ADX) rises above a key threshold, signalling a strong trend.

An optional moving‑average filter keeps trades in the direction of the broader
trend. A stop‑loss can also be enabled to cap downside risk, though by default
the system relies on signal reversals for exits.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: `+DI` crosses above `-DI` AND `ADX` > `KeyLevel` (with optional MA filter).
  - **Short**: `-DI` crosses above `+DI` AND `ADX` > `KeyLevel` (with optional MA filter).
- **Exit Criteria**: Opposite DI cross or stop‑loss if enabled.
- **Stops**: Optional stop‑loss (`UseSL`).
- **Default Values**:
  - `DILength` = 14
  - `KeyLevel` = 23
  - `UseMA` = True
  - `UseSL` = False
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: DMI, Moving Average
  - Complexity: Moderate
  - Risk level: Medium
