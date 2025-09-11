# Divergence for Many Indicators v4 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy detects divergences between price and multiple momentum indicators (MACD, RSI, Stochastic, CCI, Momentum, OBV, MFI).
A position is opened when at least a specified number of indicators show divergence in the same direction.

## Details
- **Entry Criteria**: Enter long when price falls while most indicators rise (positive divergence). Enter short when price rises while most indicators fall (negative divergence).
- **Long/Short**: Both
- **Exit Criteria**: Opposite divergence or position protection
- **Stops**: Configurable take profit and stop loss percentages
- **Default Values**: 5m candles, 2 confirmations, 4% take profit, 2% stop loss
- **Filters**: Uses several momentum indicators for confirmation
