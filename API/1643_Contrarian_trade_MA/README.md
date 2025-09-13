# Contrarian Trade MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A weekly contrarian system that evaluates prior highs, lows and a moving average to open trades at the end of each week. The position is held for one week regardless of direction.

The method is designed for major currency pairs but can be applied to any liquid asset with weekly data.

## Details

- **Entry Criteria**:
  - **Buy**: Previous week's close above the highest high of the lookback period, or the moving average is above the weekly open.
  - **Sell**: Previous week's close below the lowest low of the lookback period, or the moving average is below the weekly open.
- **Long/Short**: Both.
- **Exit Criteria**: Position is closed after being held for one week.
- **Stops**: None.
- **Timeframe**: Weekly candles.
