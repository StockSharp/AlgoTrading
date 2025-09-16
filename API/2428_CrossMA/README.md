# Cross MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades a simple moving average crossover with an ATR-based stop loss. A long position is opened when the fast SMA crosses above the slow SMA. A short position is opened when the fast SMA crosses below the slow SMA. After entering a position, a stop loss is placed one ATR away from the entry price and is checked on each new candle.

## Parameters
- Candle Type
- Fast SMA period
- Slow SMA period
- ATR period
- Volume
