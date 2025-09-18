# Prop Firm Helper Strategy

## Overview
Prop Firm Helper Strategy is a Donchian channel breakout system converted from the MetaTrader expert advisor "Prop Firm Helper". The strategy submits stop orders above the recent range for long entries and below the range for short entries. It automatically enforces prop firm challenge rules by stopping trading after the target equity is reached or when the daily loss limit is breached.

## Trading Logic
- Subscribe to candles defined by the `Candle Type` parameter.
- Calculate two Donchian channels:
  - `Entry Period`/`Entry Shift` to detect breakouts.
  - `Exit Period`/`Exit Shift` to trail open trades.
- Place buy stop orders one tick above the shifted upper Donchian high when flat or short.
- Place sell stop orders one tick below the shifted lower Donchian low when flat or long.
- Use Average True Range smoothing (`ATR Period`) to decide when to move stop orders forward.
- Close long positions if the candle settles below the trailing Donchian low. Close short positions when the candle closes above the trailing Donchian high.

## Risk Management
- `Risk Per Trade %` calculates order volume from current portfolio equity, instrument step size and step price. Volume is rounded to the exchange volume step and constrained by minimum/maximum volume.
- Protective stop orders trail the position using the exit Donchian channel plus an ATR buffer to avoid excessive order churn.

## Prop Firm Challenge Rules
- `Use Challenge Rules` enables challenge checks.
- Trading stops once `Pass Criteria` equity is reached. All orders are cancelled and the position is closed.
- Daily drawdowns greater than `Daily Loss Limit` trigger a full liquidation and disable new orders for the rest of the session. The reference equity resets at the beginning of every trading day.

## Parameters
| Name | Description |
| --- | --- |
| `Entry Period` | Lookback for breakout Donchian channel. |
| `Entry Shift` | Number of finished candles ignored when using the breakout channel. |
| `Exit Period` | Lookback for trailing Donchian channel. |
| `Exit Shift` | Number of finished candles ignored for trailing stops. |
| `Risk Per Trade %` | Percentage of portfolio equity to risk on every entry. |
| `ATR Period` | Lookback for ATR filter used when moving stops. |
| `Use Challenge Rules` | Enables prop firm challenge conditions. |
| `Pass Criteria` | Equity level that stops further trading. |
| `Daily Loss Limit` | Allowed daily drawdown before trading halts. |
| `Candle Type` | Candle subscription used for calculations. |

## Notes
- The strategy requires a portfolio connection to compute risk based position sizes and challenge metrics.
- Orders are cancelled and resubmitted on each finished candle to keep trigger prices aligned with the latest Donchian levels.
- Default parameters reproduce the behaviour of the original MetaTrader expert advisor.
