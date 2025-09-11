# Moving Average Rainbow (Stormer) Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy plots a rainbow of twelve moving averages. Trades are opened when the trend is confirmed and price touches one of the averages.

A long position opens when price makes a new high, all middle averages slope upward and the candle closes above the mean of all averages. A short position opens when the opposite conditions occur.

The stop loss is set to the previously touched moving average. The take profit is calculated as a multiple of the distance between entry price and the stop loss.

## Details

- **Indicators**: 12 moving averages of configurable type.
- **Long**: Uptrend, new high and previous touch price.
- **Short**: Downtrend, new low and previous touch price.
- **Exits**: Stop loss at touched average, target = entry ± distance * factor. Optional turnover exit when the trend shows reversal signals.
- **Parameters**: moving average type, lengths, target factor, turnover options.
- **Timeframe**: Any.
