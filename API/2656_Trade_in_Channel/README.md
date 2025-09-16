# Trade in Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Contrarian channel strategy that fades Donchian channel extremes when the band width stays unchanged. The system compares the
latest high/low against the previous channel boundaries and a pivot computed from the prior close to decide whether to fade the
move. Protective stops rely on ATR distance and an optional trailing stop maintains profits once price runs in favor of the
position.

## Details

- **Entry Criteria**:
  - Short: channel upper band unchanged and either the last candle high touched the upper band or the previous close sits between
    the pivot and the upper band.
  - Long: channel lower band unchanged and either the last candle low touched the lower band or the previous close sits between
    the pivot and the lower band.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Close long if the upper band is flat and price tags it, or if the ATR stop or trailing stop is hit.
  - Close short if the lower band is flat and price tags it, or if the ATR stop or trailing stop is hit.
- **Stops**:
  - Initial stop for longs at `support - ATR` and for shorts at `resistance + ATR`.
  - Trailing stop moves behind the best price once profit exceeds the `TrailingStopPips` distance (converted into price steps).
- **Default Values**:
  - `ChannelPeriod` = 20 (Donchian lookback)
  - `AtrPeriod` = 4 (ATR smoothing)
  - `Volume` = 1 contract/lot
  - `TrailingStopPips` = 30 price steps
  - `CandleType` = 1 hour timeframe
- **Filters**:
  - Category: Channel / Mean Reversion
  - Direction: Long & Short
  - Indicators: Donchian Channel, ATR
  - Stops: ATR hard stop + trailing stop
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

## Notes

- The pivot equals `(upper band + lower band + previous close) / 3`, matching the original MQL implementation.
- The strategy keeps only one net position and flips direction only after the previous trade is fully closed.
- Trailing distance is specified in price steps ("pips"); it is multiplied by the instrument `PriceStep` to obtain the actual
  price offset.
