# Range Breakout Strategy

The strategy measures the highest and lowest prices within the last `RangePeriod` candles. When the candle closes outside of this range and the total width of the range is narrower than `MaxRangePoints`, the strategy enters in the breakout direction.

## Entry Rules
- **Long**: Candle close >= highest high of the lookback range AND range in points <= `MaxRangePoints` AND no open position.
- **Short**: Candle close <= lowest low of the lookback range AND range in points <= `MaxRangePoints` AND no open position.

## Exit Rules
- Protective stop loss and take profit are applied immediately after the position is opened.
- No additional exit rules are used; the position stays open until protection closes it.

## Parameters
- `RangePeriod` – number of candles for highest/lowest calculation.
- `MaxRangePoints` – maximum width of the range in points to allow trading.
- `CandleType` – timeframe of candles used for analysis and trading.
- `Volume` – market order volume.
- `StopLossPoints` – stop loss distance in points.
- `TakeProfitPoints` – take profit distance in points.

## Indicators
- Highest
- Lowest