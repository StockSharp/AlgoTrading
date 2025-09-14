# Murrey Math Bollinger & Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades reversals at extreme Murrey Math lines using Bollinger Bands and a Stochastic Oscillator as confirmation.

The method computes Murrey levels from the highest and lowest prices over a configurable frame. When price approaches the 0/8 line during oversold conditions the strategy buys. When price nears the 8/8 line during overbought conditions it sells. A minimum Bollinger band width filter prevents trading in flat markets.

## Details

- **Entry Criteria**
  - **Long**: Close is within *Entry Margin* above the 0/8 line, Stochastic <= 21 and Bollinger band width >= threshold.
  - **Short**: Close is within *Entry Margin* below the 8/8 line, Stochastic >= 79 and Bollinger band width >= threshold.
- **Long/Short**: Both.
- **Exit Criteria**
  - Long positions close at the 1/8 line or if price falls below the -2/8 line.
  - Short positions close at the 7/8 line or if price rises above the +2/8 line.
- **Stops**: Murrey lines (-2/8 or +2/8) act as protective stops.
- **Filters**
  - Bollinger band width filter.
  - Stochastic oscillator filter.

