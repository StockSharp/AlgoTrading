# Bulls & Bears Power Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on the crossover of the Bulls Power and Bears Power indicators on a four-hour timeframe. The Bulls Power measures buying pressure above an average price, while Bears Power shows selling pressure below it. When buying strength overtakes selling strength, the system opens a long position. When selling strength becomes dominant, it opens a short.

Testing on historical crypto data shows that clear crossovers often precede short-term reversals. The strategy is designed to always be either long or short, reversing the position whenever the indicators cross in the opposite direction.

## Details

- **Entry Criteria**:
  - **Long**: Bulls Power value crosses above Bears Power.
  - **Short**: Bears Power value crosses above Bulls Power.
- **Long/Short**: Both sides.
- **Exit Criteria**: Opposite crossover that reverses the position.
- **Stops**: None. Positions are reversed rather than stopped out.
- **Filters**:
  - Timeframe: 4-hour candles by default.
  - Indicators: Bulls Power, Bears Power.
  - Direction: Reversal based on momentum shift.
