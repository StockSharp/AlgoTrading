# EA Trix Strategy

## Overview

The EA Trix strategy replicates the logic of the MetaTrader 5 expert advisor that combines the *TRIX ARROWS* indicator with
basic risk management tools. The system waits for the triple exponential moving average (TRIX) and its signal line to cross
before entering new positions. It can either react immediately on the signal candle or delay execution until the next bar,
emulating the original "trade at close bar" behaviour.

## Trading Logic

1. Build two triple-smoothed exponential moving averages:
   - TRIX is calculated by applying three EMAs with the **TRIX EMA** length to the candle close and taking the one-bar rate of
     change of the third smoothing.
   - The signal line is calculated the same way but uses the **Signal EMA** length.
2. Detect direction changes through crossovers:
   - When the signal line crosses **above** TRIX the strategy prepares a long entry.
   - When the signal line crosses **below** TRIX it prepares a short entry.
3. Depending on the **Trade On Close** setting the strategy will either:
   - Execute immediately at the close price of the signal bar; or
   - Queue the order and execute it at the open of the next bar (matching the MT5 EA option to trade on closed bars).
4. Before opening a new position the algorithm automatically reverses any opposing exposure so only one net position exists at
   any time.

## Position Management

- **Stop loss** – optional fixed distance from the fill price. Disabled when set to zero.
- **Take profit** – optional profit target. Disabled when set to zero.
- **Break-even** – once price advances in favour of the trade by the selected distance, the stop is moved to the entry price.
- **Trailing stop** – after price moves by the trailing distance, the stop follows price with the selected **Trailing Step**
  minimum increment.
- Protective exits are evaluated on each completed candle using the candle high/low values. When a protective exit triggers the
  position is closed with a market order.

## Parameters

| Name | Description |
| ---- | ----------- |
| `CandleType` | Data type (timeframe) of the candles processed by the strategy. |
| `Volume` | Position size used for new entries. Existing positions are reversed automatically when necessary. |
| `EmaPeriod` | Length of the exponential moving averages used to compute the TRIX curve. |
| `SignalPeriod` | Length of the exponential moving averages used to compute the signal curve. |
| `TradeOnCloseBar` | If `true`, entries are queued and executed on the next bar open. If `false`, execution happens immediately on the signal bar close. |
| `StopLoss` | Distance from the entry price to the protective stop. Set to `0` to disable. |
| `TakeProfit` | Distance to the profit target. Set to `0` to disable. |
| `TrailingStop` | Distance for the trailing stop to activate. Set to `0` to disable. |
| `TrailingStep` | Minimal increment applied when updating the trailing stop. |
| `BreakEven` | Distance required to move the stop to the entry price. Set to `0` to disable. |

## Usage Notes

- The strategy subscribes to a single candle feed and relies exclusively on completed candles as required by the StockSharp
  high-level API guidelines.
- Default risk-management distances are expressed in price units. Adjust them according to the traded instrument tick size.
- Because orders are sent via market commands the fill price is assumed to be the candle close (or open for queued signals) in
  backtests.

## Conversion Notes

- The original MQL5 expert uses the external *TRIX ARROWS* indicator (code 19056). The conversion reconstructs the same
  calculations using StockSharp `ExponentialMovingAverage` instances and rate-of-change logic without relying on custom buffers.
- MT5 risk management relied on broker-side stop and limit orders. In StockSharp protective exits are replicated by monitoring
  candle extremes and issuing market orders.
- Alerting, sound notifications and broker-specific parameters were omitted because they are not part of the core trading logic.
