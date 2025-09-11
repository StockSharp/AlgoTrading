# Screener Mean Reversion Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades a mean reversion channel built from a moving average and ATR. It sells when price closes above the upper band and buys when price closes below the lower band. Positions are closed when price returns to the mean line.

## Details
- Entry: close above upper band -> short, close below lower band -> long.
- Exit: price crossing back to the mean.
- Indicators: SMA and ATR.
- Stops: none.
