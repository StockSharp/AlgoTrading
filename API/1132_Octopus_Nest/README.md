# Octopus Nest Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy seeks squeeze breakouts using Bollinger Bands and Keltner Channels. Direction is confirmed with EMA and Parabolic SAR. Stops are placed at recent highs/lows with configurable risk reward.

## Details

- **Entry Criteria**:
  - **Long**: Price above EMA and PSAR, outside squeeze.
  - **Short**: Price below EMA and PSAR, outside squeeze.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss at recent extremes and take-profit based on risk reward ratio.
- **Stops**: Yes, fixed by recent high/low.
- **Filters**: Bollinger/Keltner squeeze, EMA trend, PSAR direction.
