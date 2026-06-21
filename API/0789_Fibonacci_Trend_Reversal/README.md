# Fibonacci Trend Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategy builds a Fibonacci channel using recent highs and lows. A position is opened when price crosses the 50% level in the breakout direction. Risk control relies on ATR based stop loss and risk reward take profits with optional partial exit.

## Parameters
- **Candle Type** — candle series.
- **Sensitivity** — base sensitivity for channel calculation.
- **ATR Period** — ATR length for stop loss.
- **ATR Multiplier** — ATR factor for stop loss.
- **Risk Reward** — take profit multiple of risk.
- **Use Partial TP** — close half position at first target.
- **Trade Direction** — allowed trade direction.
