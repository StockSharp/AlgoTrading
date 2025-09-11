# EMA Crossover Take Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on a crossover of the 20-period and 50-period exponential moving averages (EMAs). A long position is opened when the fast EMA crosses above the slow EMA, and a short position is opened on the opposite crossover.

After an entry, four take-profit levels are calculated from the range of the signal candle. The position is closed when price reaches any of these levels or when a stop-loss triggers. Candles are highlighted green when the fast EMA is above the slow EMA and red when it is below.

## Details

- **Entry Criteria**:
  - **Long**: EMA20 crosses above EMA50.
  - **Short**: EMA20 crosses below EMA50.
- **Take Profit**: Four targets based on previous range multipliers.
- **Stops**: 3% stop-loss from entry price.
- **Indicators**: EMA20, EMA50, EMA200.
- **Timeframe**: Configurable via parameter.
- **Direction**: Long and Short.

