# Intraday Momentum Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trades within a specified session using EMA crossover, RSI filter and VWAP confirmation. Goes long when the fast EMA crosses above the slow EMA, RSI is below the overbought level and price is above VWAP. Shorts on opposite conditions. Applies fixed stop-loss and take-profit percentages and closes any position at session end.

## Parameters

- **EmaFastLength**: Fast EMA length.
- **EmaSlowLength**: Slow EMA length.
- **RsiLength**: RSI period.
- **RsiOverbought**: RSI overbought level.
- **RsiOversold**: RSI oversold level.
- **StopLossPerc**: Stop loss percentage.
- **TakeProfitPerc**: Take profit percentage.
- **StartHour**: Session start hour.
- **StartMinute**: Session start minute.
- **EndHour**: Session end hour.
- **EndMinute**: Session end minute.
- **CandleType**: Type of candles.

