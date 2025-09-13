# BB Squeeze Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy monitors the contraction and expansion of Bollinger Bands to exploit volatility breakouts. It defines a squeeze as a period when the distance between the upper and lower Bollinger Bands becomes narrow relative to the middle band. Once volatility expands and price closes outside of the band after a squeeze, the system enters in the direction of the breakout.

Positions are opened with market orders. A long position is created when price closes above the upper band following a squeeze, while a short position is opened when price closes below the lower band. Only completed candles are processed, preventing premature signals during formation.

The algorithm tracks band width changes without storing entire candle histories. By comparing the current width to the previous one, it ensures that an expansion truly occurs before placing orders. This avoids entering during extended low-volatility phases where no breakout develops.

Default parameters use a 20-period Bollinger Band with a width multiplier of 2. The squeeze threshold is set to 0.05, meaning the bands must be within five percent of the middle line to register low volatility. The candle timeframe and all numerical values are fully configurable and support optimization in the StockSharp environment.
