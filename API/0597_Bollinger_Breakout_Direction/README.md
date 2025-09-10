# Bollinger Breakout Direction
[Русский](README_ru.md) | [中文](README_cn.md)

Trades Bollinger Band breakouts filtered by RSI. Direction can be long, short, or both. Uses fixed stop loss and take profit based on risk/reward ratio.

## Details

- **Data**: Price candles.
- **Entry**: Long when close is above upper band and RSI above midline; short when close below lower band and RSI below midline.
- **Exit**: Stop loss and take profit from risk/reward.
- **Instruments**: Any instruments.
- **Risk**: Configurable stop and take levels.
