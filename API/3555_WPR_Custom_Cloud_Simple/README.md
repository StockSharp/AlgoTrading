# WPR Custom Cloud Simple Strategy

## Overview
The **WPR Custom Cloud Simple Strategy** is a StockSharp port of the MetaTrader expert advisor `WPR Custom Cloud Simple.mq5`. The EA monitors Larry Williams' %R oscillator and opens trades when the indicator exits oversold or overbought territory. This C# version keeps the original design of trading only on new candles, reversing the position when an opposite signal appears, and avoids any stop-loss or take-profit orders exactly like the reference implementation.

## Trading logic
1. Subscribe to the configured timeframe (`CandleType`) and feed a `WilliamsR` indicator with the incoming candles.
2. Wait until the candle finishes; the strategy never acts on incomplete bars.
3. Store the last two completed %R values. They mirror the `wpr[1]` and `wpr[2]` readings from MetaTrader.
4. Generate signals on crossovers:
   - **Long setup**: the previous bar closes above `OversoldLevel` while the bar before it was below the level. This recreates the "exit from oversold" condition (`wpr[2] < level` and `wpr[1] > level`) from the EA.
   - **Short setup**: the previous bar closes below `OverboughtLevel` while the earlier bar was above it, matching the original `wpr[2] > level` and `wpr[1] < level` check.
5. When a long setup appears, flatten any short exposure and buy one net volume. When a short setup fires, flatten the long side and sell one net volume. Because StockSharp works with net positions, sending `BuyMarket`/`SellMarket` with `Volume + |Position|` perfectly replicates the close-and-reverse flow from MetaTrader's hedge account.
6. No additional exits are used; a new opposite crossover is the only way to close trades, just like in the original advisor.

## Parameters
| Name | Type | Default | MetaTrader counterpart | Description |
| --- | --- | --- | --- | --- |
| `WprPeriod` | `int` | `14` | `Inp_WPR_Period` | Lookback length for the Williams %R calculation. |
| `OverboughtLevel` | `decimal` | `-20` | `Inp_WPR_Level1` | Threshold that defines overbought territory. Crossing below it triggers shorts. |
| `OversoldLevel` | `decimal` | `-80` | `Inp_WPR_Level2` | Threshold that defines oversold territory. Crossing above it triggers longs. |
| `CandleType` | `DataType` | 1-hour time frame | `InpWorkingPeriod` | Candle series used to update the indicator and evaluate signals. |
| `Volume` | `decimal` | Strategy base volume | `InpLots` | Lot size for market orders. The strategy automatically offsets the current net position before opening a new trade. |

## Differences from the original EA
- StockSharp operates with net positions. Closing the opposite exposure is handled by increasing the market order volume, so the behaviour matches the hedging model without extra bookkeeping structures like `STRUCT_POSITION`.
- All order management helper classes (`CTrade`, `CPositionInfo`, margin checks, etc.) are replaced by StockSharp's built-in risk controls. The strategy relies on `Strategy.Volume` and the exchange metadata instead of manual free-margin calculations.
- Logging is simplified. The StockSharp version avoids verbose `Print` statements because the high-level API already provides order status updates.
- Protective orders are intentionally omitted to reflect the "close on opposite signal" design of the source EA.

## Usage tips
- Adjust `CandleType` to the same timeframe you used in MetaTrader to keep the crossover frequency comparable.
- Williams %R thresholds are negative values. Moving `OverboughtLevel` closer to zero makes short entries rarer, while pushing `OversoldLevel` towards `-100` makes longs rarer.
- The strategy assumes `Volume` is already aligned with the broker's minimum step and netting rules. Tune the base volume in the UI or through code before starting live trading.
