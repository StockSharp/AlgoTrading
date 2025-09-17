# A System Championship Strategy

## Overview
- Port of the MetaTrader 4 expert advisor "A System: Championship Strategy Final Edit" (file `ACB6.MQ4`).
- Detects bullish or bearish breakouts on a configurable primary timeframe and confirms momentum with live bid/ask prices.
- Uses a secondary timeframe to size the trailing stop distance, reproducing the multi-threaded logic of the original EA through two candle streams.
- Implements the global equity stop, trade pause and adaptive risk sizing blocks that were hard-coded in the source robot.

## Data subscriptions
- Subscribes to two candle series (`PrimaryTimeFrame`, `SecondaryTimeFrame`) to rebuild the price ranges used for targets and trailing stops.
- Subscribes to level 1 quotes in order to read the best bid/ask that trigger entries, stop-loss checks, take-profits and the retracement exit.

## Entry conditions
1. Wait for the primary candle to finish and compute its range multiplied by `TakeFactor`.
2. Go long when:
   - The candle closes above its midpoint.
   - The current ask price breaks the candle high.
   - The distance between the bid and the candle low exceeds `MinStopDistance`.
3. Go short when the mirrored conditions are true for the downside breakout.
4. Skip entries if the calculated take-profit distance is smaller than the minimal stop spacing.

## Exit management
- **Initial protective levels**: the stop is anchored to the previous candle low/high, while the take-profit equals the entry price plus/minus the range multiplied by `TakeFactor`.
- **Retracement exit** (`FallLimit`/`FallFactor`):
  - Track the maximum favourable excursion for the active position.
  - If the current move drops below `FallLimit * maxMove` *and* the move already advanced beyond `FallFactor * target`, close the trade at market.
- **Trailing stop** (`TrailFactor`):
  - The trailing distance equals the secondary timeframe range multiplied by `TrailFactor`.
  - The stop only moves in the trade direction and never crosses the take-profit or the minimal stop spacing.
- **Hard stops**: price touching the maintained stop or take levels results in immediate liquidation using market orders.

## Risk management
- **Dynamic position sizing**: combines `RiskPerTrade` with the pip value derived from `Security.StepSize` and `Security.StepPrice`. The resulting volume is rounded to exchange constraints and never goes below `BaseVolume`.
- **Statistical adjustment**: the ratio `LossesExpected/TradesExpected` from the original EA modulates the risk per trade by comparing it with the realised loss ratio.
- **Equity stop** (`SystemStop`): tracks the equity peak and disables new trades if the current value falls below `SystemStop * peak`. Informational logs mark the stop activation and recovery.
- **Trade pause** (`TradePause`): enforces a cool-down window after every market order, just like the MT4 implementation.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `PrimaryTimeFrame` | 1 day | Higher timeframe used for breakout detection. |
| `SecondaryTimeFrame` | 4 hours | Timeframe that provides the trailing stop range. |
| `TakeFactor` | 0.8 | Multiplier applied to the primary candle range when creating the take-profit. |
| `TrailFactor` | 10 | Multiplier applied to the secondary candle range when updating the trailing stop. |
| `FallLimit` | 0.5 | Ratio of the maximum profit that allows the retracement exit. |
| `FallFactor` | 0.4 | Minimum share of the full target that must be reached before a retracement exit is permitted. |
| `RiskPerTrade` | 0.02 | Fraction of equity allocated to each trade before adjustments. |
| `BaseVolume` | 1 | Fallback order size used when risk sizing yields a smaller volume. |
| `MinStopDistance` | 0 | Exchange stop distance constraint expressed in price units. |
| `TradePause` | 5 minutes | Waiting period after any executed order. |
| `SystemStop` | 0.8 | Drawdown factor for the portfolio equity stop (e.g. 0.8 = 20% allowable drawdown). |
| `LossesExpected` | 20 | Expected number of losing trades for risk adjustment. |
| `TradesExpected` | 85 | Expected number of total trades for risk adjustment. |

## Implementation notes
- The lock/hedging threads from the MQL version are omitted because StockSharp strategies operate on a net position. Risk control and trailing logic provide an equivalent capital protection mechanism.
- Stop and take levels are tracked inside the strategy instead of using separate native orders to stay aligned with the backtesting engine.
- Ensure that the connected security exposes `StepSize`, `StepPrice`, `MinVolume` and `VolumeStep`; otherwise, sizing falls back to `BaseVolume`.
- The strategy should run with real-time quotes enabled; otherwise only candle-driven updates will execute and stop logic will react with candle latency.
