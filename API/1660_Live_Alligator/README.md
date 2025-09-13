# Live Alligator Strategy

This strategy trades trend reversals using a dynamic Alligator configuration and several EMA filters.
It opens a new position when the Alligator lines switch direction and five EMAs confirm the move.
An optional trading-hours filter limits entries to a chosen session.
The open position is closed when price crosses a trailing smoothed moving average.

- **Entry Criteria**
  - Alligator lips above jaws with teeth below jaws and previous bar lips below jaws -> open long after a bearish trend.
  - Alligator lips below jaws with teeth above jaws and previous bar lips above jaws -> open short after a bullish trend.
  - Five EMAs on close, weighted, typical, median and open prices must be strictly ordered in the direction of the trend.
- **Exit Criteria**
  - Price crosses the trailing SMMA based on `TrailPeriod`.
  - Optional stop-loss applied at trade open.
- **Indicators Used**
  - Smoothed Moving Averages for Alligator lines and trailing stop.
  - Exponential Moving Averages on different price types.

Parameters allow configuring Alligator base period, EMA confirmation period, trailing period, stop-loss and trading hours window.
