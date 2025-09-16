# Kositbablo 10
[Русский](README_ru.md) | [中文](README_cn.md)

Multi-timeframe EURUSD strategy using RSI and EMA signals.
It checks daily and hourly indicators and opens market orders when both trend filters align.

## Parameters
- **Take Profit** – take profit in points.
- **Stop Loss** – stop loss in points.
- **Turbo Mode** – allow new trades even if a position exists.

## Rules
- Go long when Daily RSI(11) < 60, Hourly RSI(5) < 48, and EMA20 > EMA2.
- Go short when Daily RSI(22) > 38, Hourly RSI(20) > 60, and EMA23 > EMA12.
- Trades only after the hourly candle is finished.
