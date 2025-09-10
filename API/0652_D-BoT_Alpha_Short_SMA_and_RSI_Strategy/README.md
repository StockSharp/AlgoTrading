# D-BoT Alpha Short SMA and RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Short strategy that sells when RSI crosses above a threshold while price stays below a simple moving average. A trailing stop follows new lows and positions are closed if RSI reaches stop or take-profit levels.

## Details

- **Entry Criteria**: RSI crosses above the entry level and price is below the SMA.
- **Exit Criteria**: Price crosses above the trailing stop or RSI hits stop or take-profit levels.
