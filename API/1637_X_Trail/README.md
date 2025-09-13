# X Trail Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy generates trades when a fast and a slow simple moving average
built on median price cross each other. The logic mirrors the original MQL
script **X_trail.mq4** which used alerts on such crossovers.

A long position is opened when the fast MA remains above the slow MA for the
current and previous bar while being below it two bars ago. The opposite
pattern triggers a short position. Positions are reversed on every new signal.

## Details

- **Entry Criteria**:
  - **Long**: Fast MA > slow MA on the last two finished candles and fast MA was below slow MA two candles earlier.
  - **Short**: Fast MA < slow MA on the last two finished candles and fast MA was above slow MA two candles earlier.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite crossover (position reversal).
- **Stops**: None.
- **Indicators**:
  - Two simple moving averages calculated from median price.
