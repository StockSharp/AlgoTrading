# Day Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades 5-minute candles using Parabolic SAR, MACD (12,26,9), Stochastic Oscillator (5,3,3) and Momentum (14). It requires all indicators to align before entering a position.

- **Long entry**: SAR below price with previous SAR above current, Momentum < 100, MACD line below signal line, Stochastic %K < 35.
- **Short entry**: SAR above price with previous SAR below current, Momentum > 100, MACD line above signal line, Stochastic %K > 60.

Positions are closed when the opposite conditions occur. Risk management uses a trailing stop and optional take profit.

## Parameters
- **Volume** – order volume.
- **Take Profit** – target profit in points.
- **Trailing Stop** – trailing stop distance in points.
- **Candle Type** – candle subscription type (default 5-minute).
