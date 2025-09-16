# Simple Trade Copier Strategy

## Overview

The **Simple Trade Copier Strategy** replicates manual or external trades that arrive to the same
StockSharp connector and portfolio. Whenever a new trade without the configured prefix is detected,
the strategy calculates the net exposure change and submits a proportional copy trade using the
selected multiplier. Copied trades are tagged with a configurable comment prefix so that they can be
identified and managed independently.

This implementation converts the original MQL4 expert advisor `trade_copier_v.mq4` into a
StockSharp high-level strategy while keeping the behaviour of mirroring trades on the same account.
It relies on `Connector.NewMyTrade` notifications, works with any security delivered to the
portfolio, and automatically synchronises both the source and the copied exposures.

## Core logic

1. **Trade monitoring** – Each new `MyTrade` event is analysed. Trades bearing the configured
   prefix are treated as already copied trades, while any other trades are considered source
   operations that must be mirrored.
2. **Position tracking** – The strategy keeps separate dictionaries for original and copied
   positions. It also records pending adjustments to avoid submitting duplicate orders while
   awaiting fills.
3. **Order replication** – When the source and copied exposures diverge, the strategy calculates the
   difference, checks the slippage constraint, and submits a market order with the appropriate side
   and volume.
4. **Housekeeping** – Processed trade identifiers are cached for a configurable duration to prevent
   duplicated reactions when the connector resends historical trades. The legacy `CheckInterval`
   parameter controls this retention window.

## Parameters

- **Slippage** – Allowed deviation (in price ticks) between the latest trade price and the price of
  the source trade. If the difference exceeds this threshold, no copy order is submitted.
- **Multiplier** – Volume multiplier applied to every copied trade. Set this value to `1` for a
  one-to-one mirror, `2` to double the size, etc.
- **MaxOrderAge** – Maximum allowed age of the source trade. Trades older than this value are
  ignored in order to prevent delayed or historical fills from being mirrored.
- **CommentPrefix** – Prefix attached to the comment of every copied order. It is also used to
  recognise existing copies and avoid copying them again.
- **CheckInterval** – Legacy timer interval imported from the original MQL version. The StockSharp
  strategy operates in an event-driven fashion, so synchronisation occurs immediately, but this
  value defines how long processed trade identifiers are kept in memory.

## Usage guidelines

1. Attach the strategy to the same connector and portfolio that receives the original trades.
2. Configure the parameters according to the desired behaviour. In particular, adjust the multiplier
   and slippage values to match the execution requirements of the trading venue.
3. Start the strategy. It will instantly begin tracking trades and maintain the copied exposure in
   sync with the original exposure.
4. To exclude specific trades from copying, add the configured prefix to their order comment before
   execution.

## Notes

- The strategy works with any instrument supported by StockSharp. No explicit symbol list is
  required.
- Only market orders are used for the copy operation, matching the behaviour of the MQL script.
- Closing a position on the source account automatically triggers an opposite copy order so that the
  cloned exposure returns to zero.
- Because the implementation is event driven, no explicit timer is needed; nevertheless, all data
  structures are protected with locks to ensure thread safety when multiple trades arrive in quick
  succession.
