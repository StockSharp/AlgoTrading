# AIS1 Trading Robot (MQL/8700 Conversion)

## Overview
The **AIS1 Trading Robot** is a direct C# conversion of the MetaTrader 4 expert advisor from `MQL/8700/AIS1.MQ4`. The original system is tailored for EURUSD daily breakouts and uses multi-timeframe ranges for stop, target, and trailing calculations. This StockSharp implementation preserves the structure of the legacy robot while exposing every configurable element as strategy parameters.

## Trading Logic
- **Timeframes**
  - Primary candles: 1-day bars for entry conditions, stop loss, and take profit distances.
  - Secondary candles: 4-hour bars for dynamic trailing stop calculations.
- **Entry Conditions**
  - Long breakout: yesterday's daily close is above the mid-point of the bar and the current ask pierces the previous daily high.
  - Short breakout: yesterday's daily close is below the mid-point and the current bid drops under the previous daily low.
  - Only one position can be open at a time; opposite signals are ignored until the current trade is closed.
- **Initial Risk & Reward**
  - Stop loss = previous daily high/low ± `StopFactor × daily range`.
  - Take profit = entry price ± `TakeFactor × daily range`.
  - Both levels are validated against the optional `StopBufferTicks` to respect broker stop-distance constraints.
- **Trailing Stop**
  - Uses the range of the latest 4-hour candle multiplied by `TrailFactor`.
  - Trailing updates require the price to move by at least `TrailStepMultiplier × spread` beyond the existing stop and to stay away from the target by the configured buffer.
  - Drawdown protection disables trailing updates when equity falls below the reserve threshold.
- **Risk Management**
  - Lot size is derived from `OrderReserve × equity` divided by the monetary risk between entry and stop.
  - Volumes are clamped to exchange limits (`MinVolume`, `MaxVolume`, `VolumeStep`).
  - Equity monitoring keeps track of the running maximum and blocks new entries once equity drops below `AccountReserve - OrderReserve` of that peak.
- **Timing Safeguard**
  - Actions (entries or trailing updates) are separated by a mandatory five-second pause, replicating the original EA throttle.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `AccountReserve` | 0.20 | Fraction of equity that must remain untouched. Used to compute the allowed drawdown. |
| `OrderReserve` | 0.04 | Fraction of equity allocated to each trade and the basis for position sizing. |
| `PrimaryCandleType` | Daily | Candle type used for breakout logic and static targets. |
| `SecondaryCandleType` | 4 hours | Candle type used to derive trailing distances. |
| `TakeFactor` | 0.8 | Multiplier of the daily range applied to take profit. |
| `StopFactor` | 1.0 | Multiplier of the daily range applied to stop loss. |
| `TrailFactor` | 5.0 | Multiplier of the 4-hour range applied to trailing stops. |
| `TrailStepMultiplier` | 1.0 | Spread multiplier controlling how much the price must advance before a new trailing stop is set. |
| `StopBufferTicks` | 0 | Additional price steps added as safety margins around stops and targets. |

## Usage Notes
1. Assign the desired **security** (EURUSD by default) and **portfolio** before starting the strategy.
2. Ensure both the daily and 4-hour candle sources are available; otherwise the breakout and trailing modules cannot activate.
3. The strategy subscribes to the order book to obtain current bid/ask prices. In markets without a depth feed, last traded price is used as a fallback.
4. Position exits are performed via market orders when stop or target conditions are met, matching the behavior of the MetaTrader EA that modified protective orders on the server side.
5. The drawdown limiter, pause timer, and risk sizing logic can all be tuned through the exposed parameters to adapt the robot to different brokers or contract specifications.

## Differences vs. Original MQL
- Protective stops and targets are emulated by manual position closures when prices cross the stored levels (MT4 handled this via order modification).
- Risk conversion relies on `PriceStep` and `StepPrice` from the `Security` object. When such metadata is missing, the code falls back to a 1:1 monetary conversion, so users should double-check contract specifications.
- Extensive comments and parameter descriptions were added for clarity and to better integrate with StockSharp's optimization tools.

## Requirements
- StockSharp high-level API with access to candle subscriptions and order book data.
- Properly configured trading connection for order placement and portfolio valuation.

