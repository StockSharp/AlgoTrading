# USD/CHF CCI Channel Stop Strategy

## Overview

The **USD/CHF CCI Channel Stop Strategy** is a StockSharp high-level implementation of the MetaTrader 4 expert advisor `UsdChf_new`. The strategy listens for Commodity Channel Index (CCI) breakouts on the H4 timeframe and deploys pending stop orders above or below the current price. Once an order is filled, the position is protected by the same pip-based money management rules used in the original robot: a fixed stop loss, optional cancellation of stale pending orders, break-even relocation, and trailing stop management.

This conversion keeps the original execution flow but embraces the idiomatic StockSharp workflow: candle subscriptions, indicator bindings, and high-level order helpers (`BuyStop`, `SellStop`, `BuyMarket`, `SellMarket`). All risk distances are still configured in pips to remain familiar for Forex users.

## Trading Logic

1. **Indicator & Signals**
   - Calculate a CCI with the configured period on finished H4 candles.
   - Monitor the channel boundaries: `+CCI Channel` and `-CCI Channel`.
   - Detect crossovers of the current value against the previous value to generate signals.
     - Crossing **upward** through `-CCI Channel` prepares a **buy stop** above price.
     - Crossing **downward** through `+CCI Channel` prepares a **sell stop** below price.
2. **Pending Orders**
   - Stop orders are offset from the candle close by `Entry Indent (pips)` and rounded to the instrument step.
   - Only one pending order can be active at a time. Creating a new one cancels the opposite side.
   - If the market moves away by more than `Cancel Distance (pips)` the pending order is cancelled to avoid chasing price.
3. **Position Management**
   - Filled positions inherit the original stop loss distance.
   - When the trade gains at least `Break Even (pips)`, the protective stop moves to the entry price.
   - After the profit exceeds `Trailing Stop (pips)`, the stop trails the price while keeping the configured gap.
   - Opposite CCI crossovers force a position exit and place a new stop order in the fresh direction.

## Parameters

| Parameter | Description | Default | Optimizable |
|-----------|-------------|---------|-------------|
| `CandleType` | Candle series used for CCI calculations (default H4). | 4-hour time frame | No |
| `CciPeriod` | CCI averaging period. | 73 | Yes |
| `CciChannel` | Absolute CCI level forming the channel boundaries. | 120 | Yes |
| `EntryIndentPips` | Distance (in pips) between market price and pending stop order. | 30 | Yes |
| `StopLossPips` | Initial stop loss distance in pips. | 95 | Yes |
| `CancelDistancePips` | Maximum gap before cancelling pending orders. | 30 | Yes |
| `TrailingStopPips` | Trailing stop distance once activated. | 110 | Yes |
| `BreakEvenPips` | Profit required before the stop is moved to the entry level. | 60 | Yes |

All pip distances are converted to price offsets by using the instrument `PriceStep` and `Decimals`. For 3/5-digit Forex symbols the pip equals ten price steps, otherwise it equals a single step.

## Usage Notes

1. Attach the strategy to a USD/CHF security (or any instrument where pip-based risk management is relevant).
2. Set the desired trading volume through the base `Strategy.Volume` property.
3. Optionally tune the pip-based parameters to match the broker's contract specifications.
4. Run backtests in Designer/Tester to validate the behaviour before going live.

## Conversion Notes

- The MetaTrader expert iterated through raw order pools. In StockSharp the strategy stores references to the active pending orders and uses high-level cancellation helpers instead.
- Stop loss, break-even and trailing are implemented via explicit market exits because modifying broker-side orders is not part of the high-level API.
- All inline comments were translated into English and expanded for clarity.
