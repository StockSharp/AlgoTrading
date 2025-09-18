# XP Trade Manager Grid (C#)

## Overview

The **XP Trade Manager Grid** strategy is a direct port of the MetaTrader 4 expert advisor `XP Trade Manager Grid.mq4`. It automates a symmetrical grid that continually adds new positions every time the market moves a configurable number of points away from the latest filled leg. The original expert managed profits with partial take-profit levels for the first three orders, a break-even cluster when the ladder grows larger, and a global risk guard based on account percentage. The StockSharp implementation keeps the same ideas while leveraging high-level API primitives (market orders, candle subscriptions, and strategy parameters).

## Trading Logic

1. **Initial entry** – the strategy immediately opens the very first market order in the user-selected direction (sell by default). All subsequent trades are grouped into the grid ladder.
2. **Grid expansion** – whenever the close price drifts by `StepPoints` * price step beyond the most recent leg on one side, a new market order is placed in that direction provided that the total number of simultaneous legs is below `MaxOrders`.
3. **Dedicated TP for the first three legs** – the first three orders of each side inherit their unique take-profit offsets (`TakeProfit1Partitive`, `TakeProfit2`, `TakeProfit3`). Once the candle highs/lows touch those levels the leg is flattened.
4. **Break-even cluster** – when the total amount of open legs reaches four or more, the strategy calculates the weighted break-even price of the entire ladder. Depending on which side has more legs, it offsets that break-even by the corresponding total target (`TakeProfit4Total` … `TakeProfit15Total`) divided across the active orders. If price touches the calculated objective, all exposure is closed.
5. **Cycle renewal** – if the very first order of a cycle closes but the collected profit in points is still below `TakeProfit1Total`, the logic waits for the market to move by `TakeProfit1Offset` points beyond the last exit and then re-opens the initial order.
6. **Risk control** – the floating profit in account currency (realized + unrealized) is constantly compared against `RiskPercent` percent of the portfolio starting balance. If the loss threshold is breached, the entire ladder is flattened immediately.

The C# port keeps track of every filled leg internally. Partial fills are supported and hedged structures (simultaneous buys and sells) are resolved exactly like in the MQL expert: opposite fills first cancel out outstanding legs before new exposure is recorded.

## Parameters

| Name | Description |
| --- | --- |
| `CandleType` | Data type used to drive the strategy (default: 1-minute candles). |
| `OrderVolume` | Volume of each market order/leg. |
| `MaxOrders` | Maximum simultaneous legs across both directions. |
| `StepPoints` | Distance in points between consecutive grid orders. |
| `RiskPercent` | Maximum tolerable floating loss as % of the portfolio starting balance. |
| `TakeProfit1Total` | Total point goal accumulated by order #1 cycles before no automatic renewal occurs. |
| `TakeProfit1Partitive` | Take-profit distance (points) for the very first leg. |
| `TakeProfit1Offset` | Minimum retracement distance required before re-creating the first order. |
| `TakeProfit2` / `TakeProfit3` | Individual TP offsets (points) for legs #2 and #3. |
| `TakeProfit4Total` … `TakeProfit15Total` | Break-even TP totals used once the ladder size reaches the matching number of orders. |
| `InitialSide` | Direction of the very first order (Buy or Sell). |

> **Note:** All point-based inputs are automatically scaled by the security `PriceStep`, matching the original `Point()` logic from MetaTrader.

## Behaviour Compared to MetaTrader Version

* The StockSharp variant closes the first three legs via market orders instead of modifying individual take-profit values, because the high-level API does not expose direct order modification.
* Floating profit calculations rely on the instrument step and step price. Brokers with exotic contract specifications may require fine-tuning if they do not expose those fields.
* Platform-level labels shown in MT4 ("Profit pips" / "Profit currency") are not reproduced. Instead, internal cycle statistics are used to decide when to re-open the first order.

## Requirements

* Attach the strategy to a security that exposes both `PriceStep` and `StepPrice`.
* Ensure that the trading connector supports immediate-or-cancel market orders. All grid legs are executed through `BuyMarket`/`SellMarket` helper methods.

## Usage Tips

1. Start with small `OrderVolume` values when testing to evaluate how the grid behaves on your feed.
2. Carefully adjust `StepPoints` for the symbol volatility. Larger steps reduce the number of open legs and therefore drawdown.
3. Increase `TakeProfit1Offset` when trading instruments with wider spreads to avoid premature re-entries.
4. Combine the strategy with the built-in `StartProtection()` call, which monitors unexpected disconnections and reconnects gracefully.

