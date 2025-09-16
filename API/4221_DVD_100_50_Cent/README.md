# DVD 100-50 Cent Strategy

## Overview
The DVD 100-50 cent strategy is a contrarian limit-order system ported from the original MT4 expert advisor. The logic evaluates the market across four timeframes (M1, M30, H1, D1) and scores potential setups before parking buy or sell limit orders around the nearest "100 level" price grid. When the limit order is filled the strategy manages the position with pre-calculated stop-loss and take-profit levels.

## Indicators and Data
- **RAVI (Range Action Verification Index)** on H1 and D1, calculated with SMA(2) and SMA(24) on the open price.
- **Raw candle data** on M1, M30, and H1 for pattern filters such as spike rejection, consolidation checks, and momentum tests.
- **Price grid rounding** that snaps the current price to the nearest 100-level using a two-decimal rounding and a configurable 0.1-pip offset.

## Entry Logic
1. Compute the rounded "Level 100" price by rounding the last M1 close to two decimals and shifting it by `PointFromLevelGoPips` (default 50 → 5 pips).
2. Initialize an internal score (BAL) at 0 and add/subtract points according to:
   - **Trend filter:** add 10 points when H1 RAVI is below zero for long setups or above zero for shorts.
   - **Hourly spike confirmation:** add 7 points when the previous two H1 highs/lows overshoot the grid by `RiseFilterPips`.
   - **Structure alignment:** add 45 points when the current M1 close crosses back over the level and the last three H1 lows/highs stay above/below the safety buffer (`PointFromLevelGoPips ± 30 * 0.1 pip`).
   - **Volatility guards:** subtract 50 points if recent M1 highs/lows exceed `HighLevelPips` (default 600 → 60 pips) or if fast momentum bursts appear while the D1 RAVI confirms a strong directional regime.
   - **Breakout confirmation:** subtract 50 points if the last 15 H1 candles never crossed the `LowLevel2Pips` threshold.
   - **Consolidation filter:** subtract 50 points if the latest eight M30 candles all remain inside the `LowLevelPips` band.
3. Place a limit order only when the final score is at least 50 and no other exposure (position or pending order) exists.

## Order Placement
- **Buy limit:** 10 pips below the latest M1 close. Stop-loss is `StopLossPips` below the limit price, take-profit is `TakeProfitPips` above it. When the D1 RAVI shows a rising staircase between -1 and +5 over the last four days the take-profit receives an extra 25-pip extension.
- **Sell limit:** 7 pips above the latest M1 close with symmetric stop and target rules. When the D1 RAVI shows a falling staircase between -5 and -1 the target is extended by 25 pips.
- Pending orders automatically expire after `OrderExpiryMinutes` (default 20 minutes). When an order is cancelled the stored protective levels are reset.

## Position Management
- Once filled, the strategy keeps the stored stop-loss and take-profit values internally and issues market exit orders when price touches either level.
- No trailing stop is applied in the ported version; the original EA disabled the trailing logic by default.
- New trades are blocked while an active position or pending limit order exists.

## Money Management
- When `UseMoneyManagement` is enabled the lot size mimics the MT4 implementation: it scales by `TradeSizePercent` of current equity, adjusts for mini accounts, and clamps the result to `[0.1, MaxVolume]` (mini) or `[1, MaxVolume]` (standard).
- Disabling money management forces a fixed volume controlled by the `FixedVolume` parameter.
- Trading halts when portfolio equity drops below `MarginCutoff`.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `AccountIsMini` | Use mini-account volume rounding rules | `true` |
| `UseMoneyManagement` | Enable adaptive lot sizing | `true` |
| `TradeSizePercent` | Equity percentage allocated per trade | `10` |
| `FixedVolume` | Volume used when money management is off | `0.01` |
| `MaxVolume` | Maximum allowed trade volume | `4` |
| `StopLossPips` | Stop-loss distance in pips | `210` |
| `TakeProfitPips` | Take-profit distance in pips | `18` |
| `PointFromLevelGoPips` | Base level shift in 0.1 pips | `50` |
| `RiseFilterPips` | Hourly spike confirmation distance (0.1 pips) | `700` |
| `HighLevelPips` | One-minute spike rejection threshold (0.1 pips) | `600` |
| `LowLevelPips` | 30-minute consolidation band (0.1 pips) | `250` |
| `LowLevel2Pips` | Hourly breakout confirmation distance (0.1 pips) | `450` |
| `MarginCutoff` | Equity floor disabling new trades | `300` |
| `OrderExpiryMinutes` | Pending order lifetime in minutes | `20` |

## Usage Notes
- The conversion relies on finished candles from each timeframe; ensure the historical data stream provides synchronized M1, M30, H1, and D1 candles.
- The protective stop and target are executed with market orders to mirror the MT4 behaviour of attached SL/TP values.
- Because the logic is sensitive to pip size, verify that the instrument's `PriceStep` and `Decimals` properties correctly describe the quote format.
