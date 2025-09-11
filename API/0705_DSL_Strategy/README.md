[Русский](README_ru.md) | [中文](README_cn.md)

The strategy combines Discontinued Signal Lines (DSL) with ATR bands and a Beluga oscillator. A long position opens when the price stays above the DSL line for three bars and the oscillator crosses above its lower DSL line. Short positions are opened on opposite conditions. Each trade uses the corresponding DSL band as a stop and a risk-to-reward target for take profit.

## Details

- **Entry Criteria**:
  - DSL upper band above lower line for longs, lower band below upper line for shorts.
  - Candle open and close above (or below) the DSL line for three consecutive bars.
  - DSL-Beluga oscillator crossover signal.
- **Long/Short**: Long and short.
- **Exit Criteria**:
  - Stop loss at the DSL band.
  - Take profit at risk-to-reward multiple.
- **Stops**: Yes, ATR-based.
- **Default Values**:
  - `Length` = 34
  - `Offset` = 30
  - `BandsWidth` = 1
  - `RiskReward` = 1.5
  - `BelugaLength` = 10
  - `DslFastMode` = true
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: DSL, ATR, RSI
  - Stops: Yes
  - Complexity: High
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
