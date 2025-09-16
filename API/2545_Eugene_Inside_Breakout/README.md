# Eugene Inside Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Eugene Inside Breakout strategy is a direct port of the original MetaTrader expert by barabashkakvn. It focuses on pure price
action: an inside candle sequence followed by a range breakout. Confirmation levels derived from the previous candle body ensure
that the breakout develops momentum before the strategy takes a position.

## Overview

The strategy watches for a fresh high or low relative to the previous candle. Long setups require that the prior candle has a lo
w below the high of the candle before it, highlighting compression before the breakout. Short setups refuse to trade if the prev
ious candle is an inside bar, mirroring the safeguards in the source MQL logic. Orders are always executed at market with a fixe
d volume.

## Market Logic

- Emphasises breakouts of the most recent high/low to catch directional moves early.
- Uses the prior candle body to compute two one-third retracement levels (`zigLevelBuy` and `zigLevelSell`). The price must touch
  these levels, or the session must be past the configured activation hour, before an entry is allowed.
- Prevents new positions when a breakout coincides with an inside candle against the trade direction.
- Closes open positions whenever the opposite breakout signal confirms, ensuring the strategy is always flat or aligned with the
  latest signal.

## Entry Rules

### Long

1. Current candle high is greater than the previous candle high.
2. Confirmation is received when the current low pierces the one-third retracement of the prior candle body, or the current hour
   is beyond the activation hour parameter.
3. The current low must stay above the prior low while the prior low sits below the high from two candles ago.
4. No existing position is open.

### Short

1. Current candle low is lower than the previous candle low.
2. Confirmation is received when the current high tests the upper one-third retracement of the prior candle body, or the current
   hour is beyond the activation hour parameter.
3. The previous candle must not be an inside bar.
4. The current high must be below the prior high.
5. No existing position is open.

## Exit Rules

- Close long positions when a validated short breakout forms (conditions 1–3 of the short entry logic).
- Close short positions when a validated long breakout forms (conditions 1–3 of the long entry logic).

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Time frame of the candles processed by the strategy. | 1-hour candles |
| `Volume` | Order size sent with each market order. | 0.1 |
| `ActivationHour` | Hour of day after which confirmations are automatically accepted, replicating the `TimeCurrent()` filter fro
m the MQL code. | 8 |

## Notes

- The confirmation checks labelled “white bird” and “black bird” in the original script always evaluate to false because of the
  source conditions; they are preserved for parity but do not affect trading decisions.
- No additional indicators or trailing stops are used—the approach is purely price-based and flips positions on each opposite br
eakout.
