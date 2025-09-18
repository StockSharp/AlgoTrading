# Grr AL Breakout Strategy

## Overview
The **Grr AL Breakout Strategy** is a direct port of the MetaTrader expert advisor `grr-al.mq5`. It observes the first price reached at the beginning of every candle and waits for the market to move a configurable distance away from that anchor level. When the movement exceeds the threshold, the strategy fires exactly one trade for that candle, optionally reversing existing exposure.

The StockSharp implementation keeps the behaviour of the original timer-driven robot but translates it to the high-level candle subscription model. Each new candle snapshot provides the initial reference price, while subsequent updates of the same candle supply the latest close that is used as the live market price. The approach recreates the tick-by-tick breakout detection without relying on low-level event processing.

## Trading Logic
1. **Anchor detection** – when a new candle starts, the strategy stores its open price (or the first available close if the open is not yet populated) and resets the per-candle trigger.
2. **Breakout check** – as long as no trade has been executed during the current candle, the latest close is compared with the anchor. If the price rises by more than `DeltaPoints` (converted to price by the instrument point size), a short position is opened. If the price falls by the same distance, a long position is opened.
3. **Single execution per candle** – once a breakout trade fires, no additional orders are allowed until the next candle begins, mirroring the `br` flag from the original EA.
4. **Risk management** – optional stop-loss and take-profit distances are applied immediately after opening a position. If the order only reduces an opposite exposure, the protective brackets are skipped to avoid attaching stops to a flat portfolio.
5. **Position sizing** – the strategy can trade with a fixed volume or limit the order size by a fraction of the broker-reported maximum volume.

## Parameters
- `Volume` – base volume (in contracts) used when `RiskFraction` equals zero. Matches the `BASELOT` constant from the MQL version.
- `RiskFraction` – value between 0 and 1. If greater than zero, the strategy caps the order size by multiplying the broker maximum volume by this fraction and uses the smaller value between that cap and `Volume`.
- `DeltaPoints` – number of instrument points that price must move away from the candle open to trigger a trade. Equivalent to the `DELTA` constant.
- `StopLossPoints` – protective stop distance in points. Zero disables the stop, just like the `SL` constant being zero in MQL.
- `TakeProfitPoints` – take-profit distance in points. Zero disables the target and replicates the `TP` constant behaviour.
- `CandleType` – StockSharp candle descriptor that defines the timeframe for anchoring and monitoring breakouts. By default it uses five-minute time frame but can be changed to any supported period.

## Notes and Differences from MQL Version
- The original EA used tick events with a one-second timer. This port leverages the StockSharp candle subscription API, which automatically feeds the latest candle state; no manual timer management is required.
- Bid/ask differentiation is not available in the high-level interface, therefore the strategy uses the candle close as a proxy for the trade price. Stop-loss and take-profit offsets are still applied in points, matching the behaviour of MetaTrader point arithmetic.
- The risk-based volume calculation in MetaTrader relied on margin estimation for a fixed one-lot order. In this port the calculation is simplified to a maximum volume fraction so that it remains broker-agnostic.
- Because StockSharp strategies are net-position based, submitting an order in the opposite direction may flatten or reverse the exposure automatically, similar to the `OrderSend` call with netting mode in MetaTrader 5.

## Usage
1. Attach the strategy to a security and portfolio in Designer, Runner or a custom StockSharp host application.
2. Configure the desired candle timeframe, breakout distance, stop-loss, take-profit, and volume parameters.
3. Start the strategy. It will automatically subscribe to the chosen candles, monitor each new candle for a breakout move, and place market orders when the configured conditions are met.

## Original Source
- MetaTrader 5 expert advisor: `MQL/244/grr-al.mq5`
