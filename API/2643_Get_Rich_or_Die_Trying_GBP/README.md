# Get Rich or Die Trying GBP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This StockSharp strategy reproduces the behaviour of the MetaTrader expert "Get Rich or Die Trying GBP". It focuses on the busy overlap between the New York and London sessions and waits for a short burst of directional imbalance on 1-minute candles. The algorithm counts how many of the latest bars closed below their open (labelled as "up" in the original code) versus the number that closed above their open. When the counts disagree, the strategy looks for an opportunity to fade the weaker side during the first five minutes of the chosen time windows.

The system always trades a single position at a time. It enforces a 61-second cooldown after every entry, carries both a primary fixed take-profit and a tighter secondary objective, and optionally trails the stop once price moves sufficiently in favour. All distances are expressed in pips, converted internally by using the security price step (with a ×10 multiplier for 3- and 5-decimal quotes) so that the logic matches the original MT5 implementation.

## Details

- **Entry Criteria**:
  - **Long**: More candles with `Open > Close` than with `Open < Close` over the last `CountBars` 1-minute candles, current time within the first five minutes of either `22:00 + AdditionalHour` or `19:00 + AdditionalHour`, no open position, and the 61-second cooldown elapsed.
  - **Short**: More candles with `Open < Close` than with `Open > Close` under the same time restrictions and cooldown.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Primary take-profit at `TakeProfitPips` from entry and stop-loss at `StopLossPips`.
  - Early exit when floating profit reaches `SecondaryTakeProfitPips`.
  - Optional trailing stop that activates once price advances beyond `TrailingStopPips + TrailingStepPips`, shifting the stop by `TrailingStopPips` while respecting the trailing step.
- **Stops**: Fixed stop-loss, fixed take-profit, secondary take-profit, and optional trailing stop.
- **Time Filter**: Trades only during the first five minutes after the adjusted 19:00 and 22:00 hours.
- **Cooldown**: Waits at least 61 seconds after each entry before allowing a new trade.
- **Default Values**:
  - `StopLossPips` = 100
  - `TakeProfitPips` = 100
  - `SecondaryTakeProfitPips` = 40
  - `TrailingStopPips` = 30
  - `TrailingStepPips` = 5
  - `CountBars` = 18
  - `AdditionalHour` = 2
  - `MaxPositions` = 1000
  - `CandleType` = 1-minute time frame
- **Notes**:
  - `MaxPositions` is preserved for compatibility with the original expert but this port keeps only one active position at a time.
  - Pip conversion automatically adapts to 3- and 5-decimal FX symbols by multiplying the price step by 10.
  - Trailing stop logic mirrors the MT5 version: it does not move until price improves beyond both the trailing distance and the trailing step.
