# Four Bar Momentum Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Four Bar Momentum Reversal strategy enters long when the close has been below the close from `Lookback` bars ago for at least `BuyThreshold` consecutive candles within the selected time window. The position is closed once price breaks above the previous candle high.

## Details

- **Entry Criteria**: `BuyThreshold` consecutive closes below the close from `Lookback` bars ago inside the time window.
- **Exit Criteria**: Close price greater than the previous candle high.
- **Stops**: None.
- **Default Values**:
  - `BuyThreshold` = 4
  - `Lookback` = 4
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
