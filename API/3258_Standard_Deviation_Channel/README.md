# Standard Deviation Channel Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert **Standard Deviation Channel**. It plots a linear weighted moving average (LWMA) based volatility channel and trades breakouts that align with the prevailing trend. Entries are filtered by momentum strength and a MACD confirmation, while exits combine fixed targets, break-even jumps, and trailing protection.

## Indicators and Signals
- **Standard deviation channel** built from a LWMA baseline and a configurable deviation multiplier. Long setups require the upper band to slope upward; short setups require the lower band to slope downward.
- **Trend filter:** fast and slow LWMA calculated on the same candles. Longs demand `LWMA_fast > LWMA_slow`; shorts require the opposite.
- **Momentum filter:** a 14-period Momentum indicator. At least one of the last three readings must deviate from the neutral 100 level by the configured threshold.
- **MACD filter:** classic 12/26/9 configuration. Long entries need `MACD ≥ signal`, while short entries require `MACD ≤ signal`.

## Trade Management
- **Position sizing:** uses the `TradeVolume` parameter. Reversals automatically close the opposite exposure before opening the new side.
- **Take-profit & stop-loss:** expressed in pips and evaluated against the instrument `PriceStep`. The strategy issues market exits once the candle range touches the target or stop price.
- **Break-even jump:** once unrealized profit reaches `BreakEvenTriggerPips`, the stop is moved to entry plus `BreakEvenOffsetPips` (or minus for shorts).
- **Trailing stop:** after reaching `TrailingStartPips`, the stop follows price by `TrailingStepPips`, locking in gains on both sides.
- **Channel rejection exit:** if price closes back inside the channel and the slope flattens against the position, the trade is closed early.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Primary timeframe used for all calculations. |
| `TradeVolume` | Base order size. |
| `TrendLength` | LWMA lookback that defines the channel baseline. |
| `DeviationMultiplier` | Standard deviation multiplier for channel width. |
| `FastMaLength` / `SlowMaLength` | LWMA lengths for the trend filter. |
| `MomentumPeriod` | Lookback for the momentum filter. |
| `MomentumThreshold` | Minimum deviation from 100 required in any of the last three momentum values. |
| `TakeProfitPips` / `StopLossPips` | Distance of the fixed exit levels (converted using `PriceStep`). |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Controls when and how the break-even stop is activated. |
| `TrailingStartPips` / `TrailingStepPips` | Enables and sizes the trailing stop. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD configuration. |
| `MaxPositionUnits` | Maximum absolute net position; prevents over-leveraging. |

## Usage Notes
1. Attach the strategy to a security that exposes a valid `PriceStep`. Pips are converted by multiplying this step value.
2. Use `TrendLength` and `DeviationMultiplier` to adapt the channel to different markets.
3. Momentum and MACD filters can be relaxed (lower threshold, shorter periods) to increase trade frequency.
4. The trailing logic works on candle closes; intrabar spikes that do not finish beyond the thresholds are ignored.

## Differences from the Original Expert Advisor
- The MetaTrader version relies on graphical objects to read the channel slope and uses several money-management branches (martingale sizing, equity protection). This port keeps the slope check but simplifies risk control to fixed-size trades capped by `MaxPositionUnits`.
- All exits are handled with market orders at candle completion because StockSharp strategies do not directly mirror MT4 order modification APIs.
- Email and push notifications are replaced by `AddInfoLog` messages to keep the conversion self-contained.
- Equity-based account stop-outs were omitted; instead, focus is placed on per-position protection features.

## Disclaimer
This sample is intended for educational use. Always forward-test and validate the configuration before deploying it on a live account.
