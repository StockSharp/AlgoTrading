# Early Open Trend Strategy

## Overview
- Port of the MetaTrader 4 expert advisor `earlyOpenTrend.mq4` located in `MQL/9826`.
- Trades once per direction per day by comparing the current price with the daily open after a wick-based confirmation.
- Mimics the original time-window logic, including the daylight-saving offset that shifts the broker session by one or two hours.
- Uses StockSharp high-level API with candle subscriptions, automated position protection, and built-in session handling.

## Market Logic
1. Build an intraday candle series (default 15 minutes) and reconstruct the current day's open, high, and low values.
2. Determine the active DST offset: between `SummerTimeStartDay` and `WinterTimeStartDay` the strategy subtracts two hours from configured session times; otherwise one hour is subtracted. This reproduces the original `ZD` variable.
3. Only evaluate signals when the candle start time is within `[StartHour, EndHour)` after the DST correction and the strategy is flat.
4. Long setup:
   - The latest candle closed above the daily open price.
   - The distance between the daily open and the current day's low exceeds `RangeFilterPips` (converted to absolute price using the instrument pip size).
   - No long trade has been opened earlier during the same trading day.
5. Short setup:
   - The latest candle closed below the daily open price.
   - The distance between the current day's high and the daily open exceeds `RangeFilterPips`.
   - No short trade has been opened earlier during the same trading day.
6. When a signal is triggered the strategy issues a market order with volume `OrderVolume`. The trade timestamp is stored to support holding-time exits.

## Session & Exit Rules
- `EndHour` prevents new entries after the specified time (adjusted by the DST offset).
- `ClosingHour` forces the position to close once the corrected server hour reaches the configured value.
- `HoldingHours` imposes an additional maximum holding duration; once exceeded the position is closed regardless of session time.
- Each trading direction can be executed at most once per calendar day. Daily flags reset when the strategy detects a new session start.

## Risk Management
- `StopLossPips` and `TakeProfitPips` are transformed into absolute price offsets using the pip size derived from `Security.PriceStep` (5-digit symbols automatically multiply the step by 10).
- If either parameter is greater than zero the strategy enables `StartProtection` with market execution, replicating the original post-entry `OrderModify` logic.
- Outside of the forced exits described above no additional trailing logic is applied.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `OrderVolume` | 0.1 | Size of each market order. |
| `OrderType` | 0 | Direction filter: `0` = both, `1` = long only, `2` = short only. |
| `RangeFilterPips` | 1 | Minimum wick distance between the daily open and the opposite extreme before entering. |
| `TakeProfitPips` | 100 | Take-profit distance in pips (0 disables). |
| `StopLossPips` | 1000 | Stop-loss distance in pips (0 disables). |
| `StartHour` | 7 | Session start hour before DST subtraction. |
| `EndHour` | 18 | Session end hour before DST subtraction. |
| `ClosingHour` | 20 | Hour used to flatten open trades. |
| `HoldingHours` | 0 | Maximum holding time in hours (0 disables). |
| `SummerTimeStartDay` | 87 | First day-of-year that activates the two-hour DST offset. |
| `WinterTimeStartDay` | 297 | Day-of-year when the offset returns to one hour. |
| `CandleType` | 15-minute time frame | Candle series used for calculations. |

## Usage Notes
1. Attach the strategy to a security and make sure the candle type matches the data feed granularity you want to trade.
2. Adjust session hours to match the broker's server clock. The DST parameters can be tuned if the local daylight-saving regime differs from the default European schedule.
3. Configure pip-based stops and targets according to the instrument tick size; the strategy automatically converts pips using the detected pip value.
4. Start the strategy. It will update the day profile on each finished candle, evaluate entry criteria inside the session window, and enforce the single-trade-per-direction constraint.

## Differences vs. Original MQL Expert
- Uses finished candles instead of tick-level `Bid`/`Ask` checks, which slightly delays entries but keeps the logic deterministic inside StockSharp.
- Protective orders are implemented via `StartProtection` instead of manual `OrderModify` calls.
- Graphical objects and status comments from the MetaTrader chart (rectangles, labels, spread display) are omitted.
- Forced exits at session close close the position immediately instead of switching to a break-even target when underwater.

## Testing Recommendations
- Backtest with intraday data that covers the full trading session so that daily highs/lows match the live environment.
- Validate the DST configuration by simulating dates across both summer and winter periods.
- Experiment with different wick thresholds and session hours to align the behaviour with your broker's volatility profile.
