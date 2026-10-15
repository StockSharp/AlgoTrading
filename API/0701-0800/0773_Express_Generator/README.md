# Express Generator Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades a moving average crossover confirmed by RSI and MACD signals. Position size uses an ATR-based volatility factor and a fixed risk percentage. A trailing stop in pips manages exits.

## Details

- **Entry Long**: Fast SMA crosses above Slow SMA, RSI below Overbought, MACD line crosses above signal.
- **Entry Short**: Fast SMA crosses below Slow SMA, RSI above Oversold, MACD line crosses below signal.
- **Exit**: Trailing stop in pips.
- **Position sizing**: Risk % of equity divided by stop distance adjusted by ATR.
- **Indicators**: SMA, RSI, MACD, ATR.
- **Long/Short**: Both directions.
