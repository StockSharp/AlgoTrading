# XAUUSD Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades XAUUSD using EMA crossovers, RSI extremes and Bollinger Bands.
A long position is opened when the fast EMA crosses above the slow EMA, RSI is below the oversold level and price closes above the upper Bollinger Band.
Short positions are opened on the opposite conditions.
Risk management sets stop-loss and take-profit levels based on portfolio risk percentage and a take-profit to stop-loss ratio.

## Details

- **Entry**:
  - Long: fast EMA crossover up, RSI < oversold, close > upper band.
  - Short: fast EMA crossover down, RSI > overbought, close < lower band.
- **Exit**: stop-loss or take-profit calculated from risk settings.
- **Indicators**: EMA, RSI, Bollinger Bands.
