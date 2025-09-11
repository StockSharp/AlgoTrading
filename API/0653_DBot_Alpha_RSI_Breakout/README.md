# Alpha RSI Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses SMA and RSI to capture RSI crossovers above a threshold when price is above the SMA. Trailing stop activates after RSI reaches a take-profit level. Exits on RSI stop loss, reaching take profit, or trailing stop.

## Details

- **Data**: price candles.
- **Entry**: buy when RSI crosses above entry level and price is above SMA.
- **Exit**: RSI below stop level, RSI hits take profit, or price falls below trailing stop after activation.
- **Instruments**: any.
- **Risk**: RSI-based stop loss and trailing stop after profit.
