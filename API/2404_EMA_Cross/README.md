# EMA Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades the crossover of two exponential moving averages (EMAs).
A long position is opened when the fast EMA crosses above the slow EMA, while a short position is opened when the fast EMA crosses below the slow EMA.
The **Reverse** parameter swaps the EMA roles, effectively inverting the entry signals.

Every position is protected by fixed **Take Profit** and **Stop Loss** levels.
An optional **Trailing Stop** follows the price once it moves in the favorable direction, locking in profits.

The strategy processes only finished candles and uses high-level API binding for indicators and candle subscriptions.

## Parameters
- Candle type
- Fast EMA length
- Slow EMA length
- Take profit
- Stop loss
- Trailing stop
- Reverse
