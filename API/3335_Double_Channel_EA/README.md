# Double Channel EA Strategy

## Overview

The **Double Channel EA** replicates the trading logic of the MetaTrader 4 expert advisor "DoubleChannelEA_v1.2". The StockSharp p
ort adapts the custom *iDoubleChannel_v1.5* indicator and executes breakout trades when the indicator prints arrows. The strateg
y is designed for discretionary testing with configurable risk management and schedule filters.

Key characteristics:

- Custom `DoubleChannelIndicator` rebuilds the upper, lower, and middle channel buffers plus the buy/sell arrow signals.
- High-level API usage with candle subscriptions, level-one spread validation, and native order helpers.
- Optional money-management tools: stacking positions, break-even, trailing stop, take-profit, and stop-loss logic.
- Time-of-day filter and spread filter block entries outside of user-defined operating conditions.

## Trading Logic

1. Subscribe to the selected `CandleType` and feed each finished candle into the `DoubleChannelIndicator`.
2. The indicator stores a moving window of `ChannelPeriod` candles and calculates:
   - Middle line: arithmetic mean of closes.
   - Upper line: middle plus the difference of two price envelopes derived from highs and lows.
   - Lower line: middle plus the difference of complementary envelopes derived from opens and lows.
   - Arrow signals: the previous two channel positions must flip and the previous candle must close in the direction of the brea
kout. The rules match the MT4 buffer conditions.
3. Signals can be delayed by `IndicatorShift` bars to reproduce the indicator shift parameter.
4. A buy signal opens a long position (stacking allowed when `OpenEverySignal = true`). A sell signal opens a short position. Op
posite positions can be closed immediately when `CloseInSignal = true`.
5. Protective exits manage the active position on every finished candle:
   - Static stop-loss / take-profit distances expressed in absolute price units.
   - Break-even activation once price advances by `BreakEvenPoints + BreakEvenAfterPoints`.
   - Trailing stop that requires an improvement of `TrailingStepPoints` before updating.
6. Entries are rejected when:
   - The strategy is outside trading hours (`UseTimeFilter`).
   - The live spread exceeds `MaxSpreadPoints`.
   - `MaxOrders` stacked positions are already open for the current direction.

## Money Management

The order volume is calculated as:

```
volume = ManualLotSize * (AutoLotSize ? max(RiskFactor, 0.1) : 1)
```

When reversing, the strategy automatically includes the absolute opposing position to flip to the new direction in a single mar
ket order.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 15-minute time frame | Primary candle subscription. |
| `ChannelPeriod` | 14 | Lookback for the custom channel. |
| `IndicatorShift` | 0 | Delay before acting on indicator values. |
| `OpenEverySignal` | true | Allows stacking positions on consecutive signals. |
| `CloseInSignal` | false | Closes current position when an opposite arrow appears. |
| `UseTakeProfit` | false | Enables `TakeProfitPoints`. |
| `TakeProfitPoints` | 10 | Absolute price distance for the target. |
| `UseStopLoss` | false | Enables `StopLossPoints`. |
| `StopLossPoints` | 10 | Absolute price distance for the protective stop. |
| `UseTrailingStop` | false | Enables trailing logic with `TrailingStopPoints` and `TrailingStepPoints`. |
| `TrailingStopPoints` | 5 | Distance from current price to the trailing stop. |
| `TrailingStepPoints` | 1 | Minimum improvement needed before updating the trailing stop. |
| `UseBreakEven` | false | Enables break-even adjustments. |
| `BreakEvenPoints` | 4 | Target stop level once break-even activates. |
| `BreakEvenAfterPoints` | 2 | Extra profit required before activating break-even. |
| `AutoLotSize` | true | Multiplies the manual lot by `RiskFactor`. |
| `RiskFactor` | 1 | Risk multiplier applied when auto sizing. |
| `ManualLotSize` | 0.01 | Base volume when auto sizing is disabled. |
| `UseTimeFilter` | false | Enables the schedule filter. |
| `TimeStartTrade` | 0 | Trading start hour (inclusive). |
| `TimeEndTrade` | 0 | Trading end hour (exclusive). Equal start and end means no restriction. |
| `MaxOrders` | 0 | Maximum stacked positions per direction (0 = unlimited). |
| `MaxSpreadPoints` | 0 | Maximum allowed bid-ask spread in price units. |

## Notes on Conversion

- The original indicator rendered arrows by shifting values one bar ahead. The StockSharp version stores previous snapshots and 
checks the same cross-over criteria before emitting a signal on the current candle.
- Spread filtering relies on level-one data. When quotes are unavailable the strategy blocks new orders, mimicking the MQL expe
rt that refused to trade without spread information.
- Money management in MT4 used account-based calculations. For portability the volume formula was simplified to a risk multipli
er applied to the manual lot size.
- Stop-loss, take-profit, trailing stop, and break-even distances are interpreted in absolute price units (the same convention a
s other StockSharp conversions). Adjust them according to the instrument tick size.
