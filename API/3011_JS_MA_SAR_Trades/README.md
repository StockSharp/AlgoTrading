# JS MA SAR Trades Strategy

JS MA SAR Trades converts the MetaTrader 5 expert "JS MA SAR Trades" into the StockSharp high-level API. The strategy looks for
higher swing lows or lower swing highs detected via a ZigZag-style filter, confirms momentum with two moving averages, and then
enters in the direction of a Parabolic SAR breakout. Positions are protected with classic stops, optional trailing stops and an
explicit trading session filter.

## Logic Overview

1. **Swing structure** – Highest/Lowest indicators with the configured depth approximate the original ZigZag. The two most recent
   swing lows and highs are tracked. A long setup requires the latest low to be higher than the previous one (ascending structure),
   while a short setup requires the latest high to be lower than the previous one (descending structure). A deviation filter (in
   pips) and a minimum backstep (bars between pivots) prevent noise pivots from being accepted.
2. **Moving average confirmation** – Both moving averages use the same smoothing type and applied price as the MT5 version,
   including optional positive shifts (bars to the right). A long signal needs the shifted fast MA to stay above the shifted slow
   MA; a short signal requires the opposite relation.
3. **Parabolic SAR trigger** – Once the swing and moving average conditions are satisfied, the trade is executed only if the candle
   closes beyond the Parabolic SAR level: close above SAR for longs and close below for shorts. SAR flips to the other side close
   all existing positions even outside the entry window.
4. **Risk management** – Stop-loss and take-profit levels are computed in pips (converted through the instrument price step). The
   optional trailing stop mimics the MT5 logic: the stop is shifted only after the price has moved by the configured trailing stop
   plus trailing step distance from the entry price.
5. **Session filter** – When enabled, orders are allowed only between the specified start and end hours (inclusive). Protective
   exits (stop/take/trailing and SAR reversal) are still evaluated on every finished candle.

## Entry and Exit Rules

- **Long entry**: higher swing low, Parabolic SAR below the close, fast MA (with shift) above slow MA, and close within the trading
  window. The strategy buys `OrderVolume + |Position|` to close shorts and open the long position.
- **Short entry**: lower swing high, Parabolic SAR above the close, fast MA (with shift) below slow MA, and time filter satisfied.
- **Long exit**:
  - Close price crosses below Parabolic SAR;
  - Stop-loss, trailing stop, or take-profit level is hit.
- **Short exit**:
  - Close price crosses above Parabolic SAR;
  - Stop-loss, trailing stop, or take-profit level is hit.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `OrderVolume` | `1` | Base order size used for new entries; the strategy adds the absolute current position to reverse instantly. |
| `StopLossPips` | `50` | Distance in pips between entry price and stop-loss. Set to `0` to disable. |
| `TakeProfitPips` | `50` | Distance in pips between entry price and take-profit. Set to `0` to disable. |
| `TrailingStopPips` | `5` | Trailing stop distance in pips. Works together with `TrailingStepPips`. |
| `TrailingStepPips` | `5` | Extra distance the price must travel (in pips) before the trailing stop is tightened. Must be positive when trailing is enabled. |
| `UseTimeFilter` | `true` | Enable the start/end hour filter for new entries. |
| `StartHour` | `19` | Beginning of the trading window (inclusive, exchange time). |
| `EndHour` | `22` | End of the trading window (inclusive). |
| `FastMaPeriod` | `55` | Period of the fast moving average. |
| `FastMaShift` | `3` | Forward shift (in bars) applied to the fast moving average values. |
| `SlowMaPeriod` | `120` | Period of the slow moving average. |
| `SlowMaShift` | `0` | Forward shift (in bars) for the slow moving average. |
| `MaType` | `Smoothed` | Moving average smoothing method (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `Median` | Price source for both moving averages (Close, Open, High, Low, Median, Typical, Weighted). |
| `SarStep` | `0.02` | Initial acceleration factor of the Parabolic SAR. |
| `SarMax` | `0.2` | Maximum acceleration factor of the Parabolic SAR. |
| `ZigZagDepth` | `12` | Lookback window (bars) for swing detection. |
| `ZigZagDeviation` | `5` | Minimum swing size measured in pips to accept a new pivot. |
| `ZigZagBackstep` | `3` | Minimum number of bars between consecutive pivots of the same type. |
| `CandleType` | `H1` | Trading timeframe for candle subscription. |

## Notes

- The strategy keeps the protective logic active even outside the entry window, ensuring stops and SAR flips are honoured.
- The trailing stop reproduces the MT5 implementation: once price advances by `TrailingStop + TrailingStep`, the stop is moved to
  `Close - TrailingStop` for longs (mirrored for shorts).
- Moving averages are evaluated on the selected applied price; shifting emulates the MT5 indicator offset.
- Make sure the instrument has a valid `PriceStep`, otherwise pip-based distances are skipped.
