# Breakout Bars Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy detects trend reversals using the Parabolic SAR indicator. It waits for a configurable number of negative reversals before entering in the new trend direction. Distances for stop-loss and take-profit are measured either in pips or as a percentage of the entry price.

## Parameters

- **Reversal Mode** – choose between pip-based or percent-based distance calculations.
- **Delta** – minimum price movement required between reversals.
- **Negative Signals** – how many failed reversals must occur before a trade can be opened.
- **Stop Loss** – loss protection distance from the entry price.
- **Take Profit** – profit target distance from the entry price.
- **Candle Type** – candle series used for indicator calculations.

## Logic

1. Subscribe to candle data and calculate Parabolic SAR.
2. When the Parabolic SAR flips direction and price moved by at least *Delta*, store the reversal price.
3. Count negative reversals where price moved against the previous trend.
4. Once the counter reaches the **Negative Signals** value, open a position in the new trend direction.
5. Every candle checks stop-loss and take-profit levels using the selected **Reversal Mode**.
6. Positions are closed on opposite trend change or when risk limits are hit.

The strategy is suitable for trend-following breakout systems and can be optimized by adjusting delta, stop-loss, and take-profit distances.
