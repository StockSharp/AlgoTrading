# CandleStop System Tm Plus
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy built around the CandleStop custom channel indicator. The system continuously calculates delayed highest-high and lowest-low bands, waits for a completed candle to close beyond those bands, and then reacts on the following bar. It optionally enforces a maximum position lifetime and uses point-based protective stops.

## Details
- **Entry Criteria**: Previous completed candle closes above the delayed upper channel (for longs) or below the delayed lower channel (for shorts), while the current bar stays back inside the channel to avoid double-triggering.
- **Long/Short**: Symmetrical logic for both long and short trades with independent enable flags.
- **Exit Criteria**: Opposite-color CandleStop breakouts close existing positions; optional time-based exit closes trades that remain open beyond the configured number of minutes.
- **Stops**: Uses exchange step-based stop-loss and take-profit levels via `StartProtection`.
- **Default Values**:
  - `OrderVolume` = 1
  - `UpTrailPeriods` = 5, `UpTrailShift` = 5
  - `DownTrailPeriods` = 5, `DownTrailShift` = 5
  - `SignalBar` = 1
  - `StopLossPoints` = 1000, `TakeProfitPoints` = 2000
  - `MaxPositionMinutes` = 1920
  - `CandleType` = 8-hour time frame
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: CandleStop delayed channels
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Multi-hour
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

## Parameters
- `OrderVolume`: Quantity for each market entry when a new position is opened.
- `EnableLongEntry` / `EnableShortEntry`: Toggles that allow disabling new longs or shorts independently.
- `CloseLongOnBearishBreak` / `CloseShortOnBullishBreak`: Whether to close existing positions when the opposite CandleStop breakout color appears.
- `EnableTimeExit`: Turns on the maximum holding time filter.
- `MaxPositionMinutes`: Number of minutes before an open trade is force-closed; set to zero to disable even when `EnableTimeExit` is true.
- `UpTrailPeriods` & `UpTrailShift`: Lookback length and backward shift for the bullish CandleStop channel. The shift delays the Donchian-style band by several bars to emulate the original indicator timing.
- `DownTrailPeriods` & `DownTrailShift`: Equivalent parameters for the bearish channel.
- `SignalBar`: Index of the bar inspected for breakout color (1 = previous completed candle). The next older bar is used as confirmation just like in the MQL version.
- `StopLossPoints` / `TakeProfitPoints`: Protective stop distances expressed in price steps. Passed to `StartProtection` to automatically manage exits.
- `CandleType`: Primary candle series used for the strategy. Defaults to an 8-hour timeframe to match the source script.

## Implementation Notes
- The channel values are computed with `Highest` and `Lowest` indicators combined with `Shift` to reproduce the delayed bands from the original CandleStop indicator.
- Signal colors are stored in a rolling buffer to mimic the `CopyBuffer` calls of the MQL strategy and avoid duplicate entries on consecutive candles.
- Before placing orders the strategy checks for time-based exits, closes opposing positions if required, and then issues new market orders using the configured volume.
