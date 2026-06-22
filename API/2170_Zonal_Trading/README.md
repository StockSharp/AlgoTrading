# Zonal Trading Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy uses the Awesome Oscillator (AO) and the Accelerator Oscillator (AC) to capture changes in market momentum.

## Logic
- Buy when both AO and AC rise above their previous values and at least one of them has turned upward from the prior bar while both oscillators are positive.
- Sell when both AO and AC fall below their previous values and at least one of them has turned downward from the prior bar while both oscillators are negative.
- Close a long position when AO and AC turn downward.
- Close a short position when AO and AC turn upward.

## Parameters
- **Candle Type** – source candle series for calculations.
- **Take Profit** – fixed take profit value in price units.

The strategy trades a single position at a time using market orders.
