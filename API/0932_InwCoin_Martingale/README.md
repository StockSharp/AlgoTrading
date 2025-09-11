# inwCoin Martingale Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements a simple martingale approach for long positions on Bitcoin.
It supports three optional entry signals: MACD histogram crossing above zero,
Stochastic RSI %D crossing above the 20 level, or price breaking an ATR-based channel.
After each buy, the position size can double when price drops by a configured percentage.
The entire position is closed when profit reaches a specified percent above the average entry price.

## Details

- **Entry Signals**
  - **MACD Line > 0**: histogram crosses above zero.
  - **STO RSI cross up**: %D line crosses above 20 while %K is in oversold zone.
  - **ATR Channel**: close price crosses above EMA plus ATR multiplier.
- **Take profit**: position exits when price exceeds average price by the configured percent.
- **Martingale**: additional buys occur when price drops by the configured percent from average price.
- **Direction**: Long only.

