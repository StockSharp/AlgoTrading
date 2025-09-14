# RD Trend Trigger Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The RD Trend Trigger strategy uses the RD-TrendTrigger oscillator to capture trend reversals or level breakouts depending on the selected mode. In twist mode, trades follow changes in oscillator direction; in disposition mode, trades occur when the oscillator crosses predefined levels.

## Details

- **Entry Criteria**:
  - **Twist mode**: Enter long when the oscillator turns upward; enter short when it turns downward.
  - **Disposition mode**: Enter long when the oscillator rises above `HighLevel`; enter short when it falls below `LowLevel`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signals or explicit exit conditions in disposition mode when the oscillator rises above `LowLevel`.
- **Stops**: None by default; protection can be enabled externally.
- **Default Values**:
  - `Regress` = 15
  - `T3Length` = 5
  - `T3VolumeFactor` = 0.7
  - `HighLevel` = 50
  - `LowLevel` = -50
  - `Mode` = Twist
  - `CandleType` = 4-hour candles
- **Filters**:
  - Category: Trend-following
  - Direction: Long & Short
  - Indicators: Custom RD-TrendTrigger (based on highs/lows and Tillson T3)
  - Stops: Optional
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
