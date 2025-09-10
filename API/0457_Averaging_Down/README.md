# Averaging Down Strategy

This strategy opens a position when price moves outside an ATR-based band around the EMA. If the market moves against the position, the strategy adds to it using step scaled percentage deviations (DCA). Profit is taken when price returns to the averaged entry plus a fixed percent.

## Parameters
- Candle Type – candles to process.
- EMA Length – period for the EMA trend filter.
- ATR Length – period for ATR.
- ATR Mult – multiplier for ATR bands.
- TP % – take profit percentage from average entry.
- Base Deviation % – initial deviation for first DCA level.
- Step Scale – multiplier applied to deviation for each new DCA level.
- DCA Size Multiplier – multiplier for volume on each DCA order.
- Max DCA Levels – maximum number of averaging entries.
- Initial Volume – volume of the first order.
