# Swaper Strategy (API 3751)

## Overview

The **Swaper Strategy** replicates the MetaTrader expert advisor "Swaper 1.1" using StockSharp's high-level strategy API. The
original system accumulates swap gains by constantly rebalancing a synthetic portfolio between long and short exposure. This
conversion preserves the money-flow logic by reconstructing the expert's virtual balance, computing a fair value for the
underlying instrument, and aligning the open position with that target value.

## Core Logic

1. **Synthetic capital reconstruction.** The strategy recreates the MetaTrader `money` accumulator by combining the initial
   balance (`BaseUnits * BeginPrice`), realized profit from filled orders, and the unrealized portion of the current position
   scaled by `ContractMultiplier`.
2. **Fair value denominator.** The MQL expert maintains a `com` variable that grows or shrinks with active volume. The StockSharp
   port mirrors this behaviour through `BaseUnits + ContractMultiplier * Position`.
3. **Target volume calculation.** The algorithm evaluates the maximum of the last two candle highs (adjusted by the market spread)
   and the minimum of the last two lows to reproduce the MetaTrader guard-rail. A `Experts / (Experts + 1)` factor controls how
   aggressively the strategy moves towards the fair value.
4. **Position adjustments.** Depending on the computed `dt` value the strategy either
   - closes positions when the calculated adjustment is below one tenth of a lot, or
   - sells additional volume when `dt < 0`, or
   - buys additional volume when `dt >= 0`.
5. **Margin-aware lot sizing.** The helper method `GetTradableVolume` approximates `AccountFreeMargin()` checks by comparing the
   configured `MarginPerLot` with the available portfolio capital. If the requested size exceeds the available margin, the lot
   amount is floored to the nearest tenth.

The entire loop is executed on finished candles, replacing the original tick-based function while keeping the economic logic
intact.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Experts` | `1` | Weight applied to the synthetic fair value adjustment. |
| `BeginPrice` | `1.8014` | Starting price used to rebuild the virtual balance. |
| `MagicNumber` | `777` | Preserved identifier for compatibility with the MetaTrader version (logged in orders if needed). |
| `BaseUnits` | `1000` | Initial capital units used by the denominator of the fair value equation. |
| `ContractMultiplier` | `10` | Multiplier that converts price differences into account currency. |
| `MarginPerLot` | `1000` | Approximate capital required to support one lot; governs the lot reduction logic. |
| `FallbackSpreadSteps` | `1` | Spread in price steps when level-one quotes are missing. |
| `CandleType` | `1 Hour` | Primary timeframe feeding the rebalancing loop. |

## Trading Workflow

1. Subscribe to the configured candle series and level-one data.
2. Track best bid/ask quotes to obtain an accurate spread. If the feed is silent, fall back to
   `FallbackSpreadSteps * PriceStep`.
3. Recalculate the synthetic capital and denominator on every finished candle.
4. Compute `dt` using the high price path. When `dt < 0`, switch to the low price branch to emulate the original protective
   logic.
5. Use `AdjustShort` or `AdjustLong` to shrink or expand the position. When the target size is smaller than one tenth of a lot,
   close the position completely to copy the `closeby` behaviour from MetaTrader.
6. Update realized PnL inside `OnOwnTradeReceived` so that subsequent iterations use the latest balance.

## Differences vs. the MQL4 Version

- The tick-driven `start()` loop is replaced by candle processing, which avoids busy waiting while preserving the strategic
  intent.
- Order history and open trade scanning is approximated through the strategy's own trade stream instead of `OrdersHistoryTotal()`
  and `OrdersTotal()`.
- Margin checks use `Portfolio.CurrentValue` with a configurable `MarginPerLot` constant because broker-specific margin
  functions are not available in StockSharp.
- Pair-closing via `OrderCloseBy` is emulated by simply flattening the net position, consistent with the netting model of most
  StockSharp connectors.

## Usage Notes

- Configure `MarginPerLot` according to the connector's contract specifications to prevent the strategy from requesting an
  infeasible volume.
- The strategy expects candle data to provide reliable highs and lows; use a timeframe that matches the broker feed used by the
  MetaTrader version if you want identical behaviour.
- Because level-one quotes may arrive asynchronously, the strategy stores the latest spread. Ensure that both candles and level
  one subscriptions are enabled for precise replication.
