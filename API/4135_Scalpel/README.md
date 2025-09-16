# Scalpel Strategy

## Overview
The **Scalpel Strategy** is a StockSharp port of the MetaTrader 4 expert advisor `Scalpel.mq4`. The system searches for momentum breakouts on the base timeframe, confirms the move with higher-timeframe lows/highs, and filters the entries using a directional volume study built on 1-minute candles. Position management mirrors the original EA: profits are harvested with a fixed take-profit that shrinks over time, the stop-loss can trail once price has moved in favour of the trade, and every position can be force-closed after a configurable lifetime or on Friday evening.

## Trading Logic
- **Multi-timeframe trend filter**: a long signal requires that the current lows on H4, H1, and M30 candles are higher than their previous lows. Short signals demand lower highs on the same timeframes.
- **Breakout confirmation**: the strategy waits for the best ask to exceed the previous high (long) or the best bid to drop below the previous low (short) on the base timeframe. Additionally the previous three highs (or lows) must form a staircase in the breakout direction.
- **CCI window**: the Commodity Channel Index from the previous closed candle must stay within a configurable band around zero. Positive limits use a symmetric window; negative limits relax the requirement for one of the sides exactly like in the original EA.
- **Directional volume filter**: volumes from the volatility timeframe are split into two rolling blocks. A trade is allowed only if the most recent block shows more directional volume than the older block and the older block is non-zero. Negative `VolatilityWindow` values switch the filter to range-based (non-directional) accumulation.
- **Risk management**:
  - Fixed take-profit and stop-loss distances expressed in minimum price steps.
  - The take-profit level is reduced by one price step every `TakeProfitReduceMinutes` minutes that the position stays open.
  - A trailing stop activates after price has moved by `TrailingStopPoints` and then follows the move candle by candle.
  - Positions can be forcibly closed after `LiveMinutes` or at the configured `FridayCloseHour`.
  - New entries are blocked while the absolute net position equals `MaxDirectionalPositions * TradeVolume` and optionally while the re-entry cooldown is active.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TradeVolume` | `-5` | Order size. Positive values use fixed lots; negative values represent a percentage of portfolio capital converted to volume using the current ask price. |
| `TakeProfitPoints` | `40` | Distance from entry to the take-profit target in price steps. |
| `StopLossPoints` | `340` | Distance from entry to the stop-loss in price steps. |
| `TrailingStopPoints` | `25` | Trailing stop distance in price steps. The trail engages once the move exceeds this distance. |
| `CciPeriod` | `14` | Lookback period for the Commodity Channel Index calculated on the base timeframe. |
| `CciLimit` | `75` | Upper bound for long entries and mirrored negative bound for short entries. Negative values reproduce the asymmetric limits from the original EA. |
| `MaxDirectionalPositions` | `1` | Maximum net position units (in multiples of the calculated trade volume) allowed in one direction. |
| `ReentryIntervalMinutes` | `0` | Minimum number of minutes to wait between two consecutive entries. |
| `TakeProfitReduceMinutes` | `600` | Minutes before the take-profit threshold is reduced by one price step. Set to `0` to disable the reduction. |
| `LiveMinutes` | `0` | Maximum lifetime of a position in minutes. A value of `0` disables the timer. |
| `VolatilityWindow` | `100` | Number of volatility candles stored in each rolling block. Negative values switch to range-based accumulation, `0` uses only the latest candle. |
| `VolatilityThresholdPoints` | `1` | Minimum candle body (positive window) or range (non-directional window) required to accumulate volume. The sign flips the interpretation of up/down volumes. |
| `FridayCloseHour` | `22` | Hour of day (0-23) used to liquidate positions on Friday evenings. `0` disables the Friday exit. |
| `SpreadLimitPoints` | `5.5` | Maximum allowed spread in price steps when opening a new position. |
| `CandleType` | `1 minute` | Base timeframe that generates entries and manages exits. |
| `Hour1CandleType` | `1 hour` | Higher timeframe used for H1 trend confirmation. |
| `Hour4CandleType` | `4 hours` | Higher timeframe used for H4 trend confirmation. |
| `Minute30CandleType` | `30 minutes` | Higher timeframe used for M30 trend confirmation. |
| `VolatilityCandleType` | `1 minute` | Timeframe that feeds the directional volume filter. |

## Implementation Notes
- The strategy subscribes to the order book to reuse the latest best bid/ask prices for breakout detection and spread filtering.
- All indicator bindings rely on StockSharp's high-level API: the CCI value is obtained through `BindEx`, while higher timeframes use dedicated subscriptions.
- Trailing stops and take-profit reductions are executed in code rather than via protective orders to mimic the original EA behaviour.
- Negative `TradeVolume` values rely on the current ask price and security volume constraints. When the calculated size falls below the minimum lot, it is automatically rounded up.

## Usage
1. Attach the strategy to a portfolio and choose the desired security.
2. Configure the timeframe parameters, risk thresholds, and volume sizing rules.
3. Start the strategy. Signals are evaluated on finished candles only; positions are opened with market orders and closed via the built-in risk management rules.
