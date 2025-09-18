# RAVIiAO Strategy (StockSharp)

## Overview
The **RAVIiAO Strategy** reproduces the MetaTrader 4 expert advisor "RAVIiAO" inside the StockSharp high level API. The system
waits for a new candle to close, evaluates the RAVI oscillator slope together with Bill Williams' Acceleration/Deceleration (AC)
oscillator, and opens a position immediately at market when both indicators agree on the trend direction. The port keeps the
original parameter set – moving average periods, threshold, stop-loss/take-profit distances, and order volume – allowing traders
to replicate the legacy behaviour without manual tweaks.

## Core Workflow
1. **Candle subscription** – the strategy subscribes to a configurable time-frame (30-minute candles by default).
2. **Indicator updates** – on each finished candle it updates two simple moving averages to build the RAVI oscillator and feeds
   the same candle into an Awesome Oscillator + 5-period smoothing pair to obtain the AC value.
3. **Signal preparation** – the latest finished candle is stored as "bar 1" while the prior value becomes "bar 2", matching the
   `iCustom(...,1)` and `iCustom(...,2)` calls from MetaTrader.
4. **Entry decision** – a long position is opened when both AC and RAVI increase above their previous values and confirm a
   bullish environment (`AC[1] > AC[2] > 0` and `RAVI[1] > RAVI[2] > Threshold`). Short trades use the mirrored conditions.
5. **Risk management** – as soon as an order executes the strategy records static stop-loss and take-profit levels expressed in
   instrument points (i.e. `StopLossPoints * PriceStep`). Candles are monitored for intrabar breaches using their high/low prices.
6. **State reset** – when a protective level is hit the position is closed with a market order and the internal buffers are reset
   for the next opportunity.

## Trading Rules
- **Long entries**
  - Previous AC value above the earlier AC value and both greater than zero.
  - Previous RAVI reading above both the threshold and the earlier RAVI value.
  - No active position at the moment of the signal.
- **Short entries**
  - Previous AC value below the earlier AC value and both below zero.
  - Previous RAVI reading below the negative threshold and below the earlier RAVI value.
  - No active position when the signal fires.
- **Position exits**
  - Static stop-loss and take-profit levels are expressed in raw points, converted to price offsets via the instrument `PriceStep`.
  - Breaches are detected with candle extremes (low for long stops, high for short stops, etc.) and closed immediately via market
    orders to emulate MetaTrader's protective orders.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Time-frame used for candle subscription (default 30 minutes). |
| `FastLength` | Fast moving average length used in the RAVI oscillator. |
| `SlowLength` | Slow moving average length used in the RAVI oscillator. |
| `Threshold` | Minimum absolute RAVI percentage to validate a trend continuation. |
| `StopLossPoints` | Stop-loss distance in instrument points (multiplied by `PriceStep`). |
| `TakeProfitPoints` | Take-profit distance in instrument points. |
| `TradeVolume` | Market order volume for every entry. |

## Conversion Notes
- The StockSharp port stores the two most recent indicator values so that the decision at candle *n* reuses the `AC[1]` and
  `RAVI[1]` values from MetaTrader (i.e. results of the previous bar), preserving the "new bar" execution style of the EA.
- AC is rebuilt through the difference between the Awesome Oscillator and its 5-period simple moving average, matching the MT4
  calculation chain.
- Stops and targets are evaluated against candle extremes instead of placing pending protective orders; this mirrors the effect
  of MetaTrader's built-in SL/TP handling while keeping the implementation idiomatic for StockSharp.

## Usage Tips
- Ensure the selected instrument exposes a correct `PriceStep`; otherwise protective distances will not match the MT4 version.
- Optimise the `Threshold`, `FastLength`, and `SlowLength` parameters when adapting the strategy to markets with different
  volatility characteristics.
- Combine the strategy with StockSharp portfolio- or connector-level protections for additional safety during live trading.
