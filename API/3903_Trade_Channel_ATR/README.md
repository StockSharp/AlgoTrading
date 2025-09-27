# Trade Channel ATR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Trade Channel strategy replicates the original MetaTrader expert advisor that traded price channels with ATR-based stops. It waits for channel boundaries to remain unchanged and for the latest candle to touch or reject those levels. When the setup appears, the strategy opens a position in the opposite direction of the touch and applies an adaptive trailing stop measured in points.

The approach seeks to exploit mean-reversion around a stable price channel. It filters signals so that the channel must be flat (no new highs or lows) before it enters. Protective stops are placed beyond the channel using the Average True Range, and an optional trailing stop locks in profits once the move develops.

## Details

- **Entry Criteria**:
  - Short: Channel high equals the previous channel high and the latest candle either breaks that high or closes between the high and pivot `(high + low + close) / 3`.
  - Long: Channel low equals the previous channel low and the latest candle either breaks that low or closes between the low and pivot.
- **Long/Short**: Both directions, but only one position at a time.
- **Exit Criteria**:
  - Long: Price touches the channel high while the high stayed unchanged.
  - Short: Price touches the channel low while the low stayed unchanged.
  - Optional trailing stop tightens behind the market once profit exceeds `TrailingDistance` points.
- **Stops**: Initial stop loss at `channel boundary ± ATR`. Trailing stop replaces it when activated.
- **Default Values**:
  - `Volume` = 0.1m
  - `ChannelPeriod` = 20
  - `AtrPeriod` = 4
  - `TrailingDistance` = 30
  - `CandleType` = 30-minute candles
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Highest, Lowest, Average True Range
  - Stops: ATR stop, Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday (30 minutes)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

## Notes

- `Volume` controls the order size; only one position can exist at a time.
- `TrailingDistance` is specified in points (price steps). Set to zero to disable the trailing stop.
- The strategy requires historical candles to warm up the Highest/Lowest and ATR indicators before trading.
- Stop orders are automatically canceled when the position closes or the strategy resets.
