# Aver4 Stoch Post ZigZag
[Русский](README_ru.md) | [中文](README_cn.md)

Combines four Stochastic oscillators across multiple time horizons and a simple ZigZag pivot detector. The average Stochastic guides overbought/oversold levels while the ZigZag confirms swing highs and lows. Buys occur when the averaged Stochastic falls below the oversold level and a new ZigZag low forms. Sells occur when the averaged Stochastic rises above the overbought level and a new ZigZag high forms. Existing opposite positions are closed on signal reversal.

## Details
- **Entry Criteria**: Averaged Stochastic crossing oversold/overbought with matching ZigZag pivot.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: StartProtection 2%/2% (default).
- **Default Values**:
  - `ShortLength` = 26
  - `MidLength1` = 72
  - `MidLength2` = 144
  - `LongLength` = 288
  - `ZigZagDepth` = 14
  - `Oversold` = 5
  - `Overbought` = 95
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Stochastic, ZigZag
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
