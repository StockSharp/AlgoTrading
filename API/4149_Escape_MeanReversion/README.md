# Escape Mean Reversion Strategy

## Overview
The Escape strategy is a StockSharp port of the MetaTrader 4 expert advisor `escape.mq4`. The original robot trades a five-minute chart and reacts to mean-reversion opportunities: it buys when price falls below a short moving average and sells when price rallies above another fast average. Each position is protected by a fixed-distance take-profit and stop-loss expressed in MetaTrader points. The C# implementation keeps the same minimalist logic while exposing all tunable distances as strategy parameters.

## Trading Logic
1. **Initialization**
   - Subscribe to the configurable `CandleType` series (five-minute candles by default).
   - Create two `SimpleMovingAverage` indicators with lengths 5 and 4 that are fed with candle open prices.
   - Compute the MetaTrader `Point` equivalent from `Security.PriceStep`; this value is reused to convert pip-style distances into absolute prices.

2. **Per-candle processing**
   - Only finished candles are processed via `SubscribeCandles(...).WhenCandlesFinished(ProcessCandle)`.
   - The strategy first checks whether an existing position hit its stop-loss or take-profit by comparing the candle high/low with the recorded exit levels. When a level is breached the position is closed with a market order and duplicate exit orders are prevented through internal flags.
   - If the account is flat, previous values of the two SMAs are available, trading is allowed, and the portfolio holds enough capital (`Portfolio.CurrentValue >= MinimumMarginPerLot * TradeVolume`), the strategy evaluates entries:
     * **Long entry** – current close is below the previous 5-period SMA of opens.
     * **Short entry** – current close is above the previous 4-period SMA of opens.
   - When a signal triggers, the stop-loss and take-profit levels are calculated from the candle close using the configured point distances and stored for later monitoring.

3. **Risk management**
   - `TradeVolume` defines the lot size of every market order.
   - `MinimumMarginPerLot` approximates the `AccountFreeMargin` check from MetaTrader. If the available portfolio value is too small the entry is skipped and a diagnostic message is logged.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `LongTakeProfitPoints` | `10` | Take-profit distance for long positions in MetaTrader points. Set to `0` to disable the target. |
| `ShortTakeProfitPoints` | `10` | Take-profit distance for short positions in MetaTrader points. Set to `0` to disable the target. |
| `LongStopLossPoints` | `1000` | Stop-loss distance for long positions in MetaTrader points. Set to `0` to disable the protective stop. |
| `ShortStopLossPoints` | `1000` | Stop-loss distance for short positions in MetaTrader points. Set to `0` to disable the protective stop. |
| `TradeVolume` | `0.2` | Lot size used when sending market orders. |
| `MinimumMarginPerLot` | `500` | Approximate capital requirement per lot before opening a new trade. |
| `CandleType` | Five-minute time frame | Candle series that drives indicator updates and signal generation. |

## Implementation Notes
- Indicators are updated manually inside `ProcessCandle` with candle open prices so that the stored values always represent the previous bar (mirroring the `shift=1` arguments used in `iMA`).
- Exit levels are tracked in decimal fields instead of creating additional collections, satisfying the high-level API guidelines.
- Stops and targets are evaluated against candle extremes; because only OHLC data is available, the stop check is performed before the take-profit to emulate MetaTrader’s order priority as closely as possible.
- The strategy draws candles together with both moving averages and own trades when a chart area is available, simplifying visual validation.

## Differences vs. the MetaTrader version
- MetaTrader attaches stop-loss and take-profit orders directly to tickets. The StockSharp port reproduces them by monitoring candle highs and lows and sending market exits; intrabar execution order cannot be guaranteed if both levels are touched within the same bar.
- Entry prices are derived from the candle close that triggered the signal instead of the exact bid/ask used by MetaTrader, so slippage and spread handling must be configured at the connector level.
- The `AccountFreeMargin()` guard is approximated through `Portfolio.CurrentValue`. Users with more detailed margin models can extend `HasSufficientMargin` if needed.
- Cosmetic MQL settings such as colors, sounds, and slippage are omitted; the StockSharp version focuses on the core trading logic.
