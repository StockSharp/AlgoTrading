# Martingale MACD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the original MQL "MartGreg_1" expert in the StockSharp framework. It uses two Moving Average Convergence Divergence (MACD) indicators to search for reversals and applies a martingale rule for position sizing.

## How it works

- The first MACD searches for local extremes on the last three finished candles.
- The second MACD compares the last two values to determine momentum direction.
- A long position is opened when the first MACD forms a valley and the second MACD decreases.
- A short position is opened when the first MACD forms a peak and the second MACD increases.
- After every losing trade the next order size is doubled up to the configured limit.
- Stop loss and take profit are set in absolute price points.

## Parameters

- `Shape` – divider for calculating initial volume from account balance.
- `Doubling Count` – maximum number of consecutive doublings after losses.
- `Stop Loss` – protective stop in points.
- `Take Profit` – profit target in points.
- `MACD1 Fast/Slow` – periods for the first MACD.
- `MACD2 Fast/Slow` – periods for the second MACD.
- `Candle Type` – timeframe for analysis.

