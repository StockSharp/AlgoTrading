# AeroSpine Strategy

## Overview
The AeroSpine Strategy is a conversion of the MetaTrader expert **AEROSPINE.mq4**. It trades a single symbol and attempts to capture breakouts away from the daily open price. The original robot was designed for daily charts while monitoring ticks; the port keeps the daily open breakout idea but relies on finished candles supplied by StockSharp.

## Trading Logic
- At the start of every trading day the strategy stores the daily open price derived from the first candle of the day.
- New positions are evaluated only after the configured entry hour. Finished candles must satisfy a minimum volume filter and the current spread must be below the configured limit.
- If no position is open and no recovery trade is pending:
  - A **long** trade is opened once the candle high crosses the daily open by `EntryOffsetPips`.
  - A **short** trade is opened once the candle low breaks below the daily open by `EntryOffsetPips`.
- After any losing trade the strategy prepares a recovery entry in the opposite direction. Recovery trades use `RecoveryOffsetPips` and increase the volume by adding the base volume to the size of the losing trade, replicating the martingale-style sizing from the MQL expert.
- Open positions are managed with three mechanisms:
  - A fixed take-profit at `TakeProfitPips` from the entry price.
  - An optional break-even trigger that closes the trade once price retreats back to the break-even distance after having moved in favour of the position.
  - A protective exit if price returns to the daily open and crosses it by `ExitOffsetPips` against the position.

## Parameters
| Name | Description |
| ---- | ----------- |
| **Candle Type** | Time-frame of the working candles used for signal evaluation. |
| **Volume** | Base order size used for first entries and to build the recovery volume. |
| **Entry Hour** | Minimum hour (exchange time) when new entries may be taken. |
| **Entry Offset** | Distance in pips from the daily open that must be crossed to open the first trade of the day. |
| **Exit Offset** | Distance in pips beyond the daily open used to close positions that revert back across the open. |
| **Recovery Offset** | Distance in pips from the daily open required to trigger a recovery trade after a loss. |
| **Take Profit** | Fixed take-profit distance measured in pips from the entry price. |
| **Break Even** | Distance in pips required to arm the break-even exit. |
| **Use Break Even** | Enables or disables the break-even management block. |
| **Volume Filter** | Minimum candle volume required for new entries, mirroring the original `Volume[0] > 10000` check. |
| **Max Spread** | Rejects new entries if the current spread is wider than the allowed value (converted from pips). |
| **Enable Recovery** | Enables the opposite-direction recovery logic after a losing trade. |

## Notes on the Conversion
- The original EA placed orders directly on ticks while enforcing a daily chart. The port emulates this with intraday candles: the daily open is refreshed at the first candle of each day and the breakout checks use candle highs/lows.
- All MetaTrader interface elements (labels, equity calculations across multiple symbols, etc.) were dropped. Only the trading logic relevant to the current symbol was preserved.
- Break-even and stop modifications (`OrderModify`) are simulated via explicit `ClosePosition()` calls when the calculated thresholds are touched.
- Spread and volume filters map directly to the original `MODE_SPREAD` and `Volume[0]` checks.
