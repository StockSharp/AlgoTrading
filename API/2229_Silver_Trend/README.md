# Silver Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A trend-following strategy based on the custom SilverTrend indicator. The indicator builds a dynamic price channel using the highest high and lowest low over a lookback window and a risk factor. A trading signal occurs when price crosses the channel and the trend direction reverses.

## Details

- **Entry**: Buy when the indicator switches to an uptrend. Sell when the indicator switches to a downtrend.
- **Exit**: Position reverses on the opposite signal.
- **Indicators**: Highest, Lowest, SimpleMovingAverage (inside the SilverTrend calculation).
- **Stops**: None.
- **Default Values**:
  - `Ssp` = 9 — number of bars for channel calculation.
  - `Risk` = 3 — percentage that shrinks the channel width.
  - `CandleType` = 1 hour candles.
- **Direction**: Both long and short.

The SilverTrend indicator computes the average high-low range over `Ssp + 1` bars and finds the highest high and lowest low over `Ssp` bars. The channel boundaries are:

```
smin = minLow + (maxHigh - minLow) * (33 - Risk) / 100
smax = maxHigh - (maxHigh - minLow) * (33 - Risk) / 100
```

If the close falls below `smin`, the trend turns bearish. If the close rises above `smax`, the trend turns bullish. A signal is generated when the trend flips, and the strategy immediately reverses its position accordingly.
