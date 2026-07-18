# Dema RSI Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades crossovers between the RSI of a double exponential moving average and its smoothed value. A long position opens when the RSI crosses above the smoothed line and a short position opens on the opposite crossover. Positions can be protected with take profit, trailing stop and an optional trading session filter.

## Parameters
- Candle Type
- MA length
- RSI length
- RSI smoothing length
- Take profit points
- Trail stop points
- Use session
- Session start
- Session end
