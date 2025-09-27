# TDSGlobal Pending Strategy (C#)

## Overview

This strategy ports the MetaTrader 5 expert advisor **TDSGlobal** from `MQL/23255/TDSGlobal.mq5` to the StockSharp high-level API. It evaluates momentum on four-hour candles through the MACD line, the MACD histogram (OsMA) and the Force Index. When the indicator combination signals a potential reversal, the strategy submits pending limit orders around the previous candle extremums and manages the resulting position with optional stop-loss, take-profit and trailing-stop logic.

The implementation reproduces the original workflow while adapting it to idiomatic StockSharp constructs such as `StrategyParam<T>`, candle subscriptions via `SubscribeCandles`, and asynchronous order handling through the strategy life cycle events.

## Trading Logic

1. **Indicator calculations**
   - `MACD(12, 26, 9)` provides both the MACD line and the histogram (OsMA).
   - `ForceIndex(24)` measures the force of the last completed candle.
   - Each indicator is updated on the close of the selected candle type (default: 4-hour).
2. **Signal detection**
   - The algorithm waits until two historical MACD and OsMA values are available to determine their slope.
   - A *sell* setup requires OsMA to increase (`osma[1] > osma[2]`) while the Force Index of the previous candle is negative.
   - A *buy* setup requires OsMA to decrease (`osma[1] < osma[2]`) while the previous Force Index is positive.
3. **Order placement**
   - Sell limit orders are placed slightly above the previous candle high; buy limit orders slightly below the previous candle low.
   - If the price is not sufficiently far from the current bid/ask, the order price is pulled to the configured offset buffer (`EntryOffsetPips`, default 16 pips).
   - The strategy checks that the distance between the order price and the current bid/ask exceeds the broker safety level approximation (`MinDistancePips` or the dynamic spread-based value).
4. **Risk controls**
   - Optional stop-loss and take-profit levels are computed from the order price.
   - When a position is active, a trailing stop can advance by the configured step once price moves beyond the initial trailing distance.
   - If price hits the protective levels inside a candle, the position is closed with a market order to mimic MetaTrader behaviour.
5. **Order maintenance**
   - Pending orders are cancelled when the OsMA slope turns against the original setup, matching the source EA’s clean-up routine.
   - Filling one side automatically cancels the opposite pending order to avoid conflicting exposures.

## Money Management

Two position sizing approaches are available:

- **Fixed volume** (default `OrderVolume = 1`) — uses the base `Strategy.Volume` without adjustments.
- **Risk-based sizing** — when `UseRiskSizing` is enabled, the strategy estimates the portfolio equity, converts the configured risk percentage into currency risk, and divides it by the stop-loss distance to derive the order volume. Volumes are aligned to the instrument’s volume step to avoid invalid order sizes.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Fixed order size when risk sizing is disabled. | 1 |
| `UseRiskSizing` | Enable money management based on `RiskPercent`. | true |
| `RiskPercent` | Percentage of portfolio equity risked per trade. | 3 |
| `MacdFastPeriod` | Fast EMA length for the MACD line. | 12 |
| `MacdSlowPeriod` | Slow EMA length for the MACD line. | 26 |
| `MacdSignalPeriod` | Signal EMA length for the MACD histogram. | 9 |
| `ForceLength` | EMA smoothing length for the Force Index. | 24 |
| `StopLossPips` | Stop-loss distance in pips (0 disables). | 50 |
| `TakeProfitPips` | Take-profit distance in pips (0 disables). | 50 |
| `TrailingStopPips` | Trailing stop distance in pips (0 disables). | 5 |
| `TrailingStepPips` | Minimum step for trailing updates. | 5 |
| `EntryOffsetPips` | Buffer added around previous highs/lows for pending orders. | 16 |
| `MinDistancePips` | Minimum allowed distance between price and protective levels. | 3 |
| `PipSize` | Pip size used for pip-to-price conversions. | 0.0001 |
| `CandleType` | Candle type processed by the strategy. | 4-hour candles |

## Usage Notes

1. Add the file `CS/TdsGlobalPendingStrategy.cs` to your StockSharp project or load it dynamically through the Backtester environment.
2. Assign the desired security and portfolio before starting the strategy. If `UseRiskSizing` is enabled, ensure the portfolio provides current equity values.
3. The strategy requires at least two completed candles to initialise MACD/OsMA slopes. Expect a brief warm-up phase.
4. Monitor logs for detailed order and position events. The implementation logs key actions (order submission, cancellation, trailing updates) to aid verification against the original EA behaviour.

## Differences from the MQL Version

- The high-level API manages asynchronous order events, so limit order fills are handled via `OnOwnTradeReceived` instead of synchronous `OrderSend` results.
- Broker “freeze” and “stops” levels are approximated using the configured minimum distance and a spread-based heuristic because StockSharp does not expose MetaTrader-specific trading limits.
- Protective exits execute via market orders when the candle shows a breach. This replicates the EA’s manual stop modification logic without relying on MT5 trade server constraints.

These adjustments keep the trading logic faithful while ensuring the strategy integrates smoothly with the StockSharp framework.
