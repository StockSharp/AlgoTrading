# Buy Only EMA BB Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy opens a long position when price closes above the EMA.
The initial stop loss is placed at the lower Bollinger Band and moves to the EMA if price closes above the upper band.
Take profit is set using a reward-to-risk ratio based on the distance to the band.
After take profit is hit, the strategy waits for price to cross below the EMA before a new entry is allowed.

## Details
- **Entry Criteria:** Close above EMA with no active block and no open position.
- **Long/Short:** Long only.
- **Exit Criteria:** Price crosses below the stop level or reaches the take profit.
- **Stops:** Initial stop at lower band, shifting to EMA after a strong move.
- **Default Values:** EMA length = 40, band deviation = 0.7, reward-to-risk ratio = 3.
