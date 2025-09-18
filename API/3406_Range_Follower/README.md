# Range Follower Strategy

## Overview
The Range Follower strategy reproduces the MetaTrader 5 expert advisor "Range Follower" using the StockSharp high-level API. It monitors the current day's price range relative to a daily Average True Range (ATR) benchmark and opens a single breakout trade when price travels far enough away from the session high or low. The conversion keeps the original approach of splitting the ATR into a trigger portion and a residual portion that becomes the take-profit distance.

## Trading Logic
1. **Daily volatility baseline**
   - A 20-period ATR calculated on daily candles provides the baseline range for the current trading day.
   - The ATR value is split by `TriggerPercent` into two segments: the trigger distance that must be exceeded before entering, and the remaining distance that is used as the profit target.
2. **Range tracking**
   - The strategy continuously records the current session high and low from the active daily candle.
   - Level1 updates supply the latest best bid and best ask prices that are used to measure the distance from the current quotes to the session extremes.
3. **Single entry per day**
   - When the best bid is more than the trigger distance above the session low and no trade has been opened yet, the strategy buys at market.
   - When the best ask is more than the trigger distance below the session high and no trade has been opened yet, the strategy sells at market.
   - Only one trade is allowed per day; the flag resets when a new session starts.
4. **Stop-loss and take-profit**
   - For long positions, the stop-loss is placed one trigger distance below the entry price and the take-profit one residual distance above it.
   - For short positions, the stop-loss is one trigger distance above the entry price and the take-profit one residual distance below it.
   - Price monitoring is performed on both Level1 ticks and candle updates to close positions as soon as a level is breached.
5. **Daily session reset**
   - At the first candle of a new trading day the strategy closes any open position, clears internal state, and reloads the ATR baseline.
   - If the current daily range already exceeds the trigger distance when the session is initialised, trading is skipped for the rest of the day to mimic the original EA's safety check.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 15-minute candles | Working timeframe used to detect session boundaries. |
| `TriggerPercent` | 60 | Percentage of the daily ATR used as the breakout trigger distance. Must stay between 10 and 90. |
| `Volume` | 0.1 | Market order volume for both long and short entries. |

## Risk Management
- Stops and targets are derived from the same ATR baseline so that the reward-to-risk ratio always equals `(100 - TriggerPercent) : TriggerPercent`.
- The strategy registers a single position at a time and immediately liquidates it when the stop or target is touched, preventing multiple overlapping trades.
- `StartProtection()` enables StockSharp's protective infrastructure, allowing external components to attach trailing stops or portfolio guards if required.

## Implementation Notes
- Daily ATR values are produced by a dedicated daily candle subscription and the `AverageTrueRange` indicator bound through the high-level API.
- Level1 data is necessary to mirror the EA's tick-driven decisions; best bid and best ask prices drive both entries and exit checks.
- Daily session boundaries are derived from the working timeframe candles, ensuring that any trading calendar used in StockSharp will reset the strategy consistently.
- The conversion avoids manual indicator buffers or historical loops, relying instead on stateful fields updated by the `Bind` callbacks.
