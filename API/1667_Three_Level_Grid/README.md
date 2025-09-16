# Three Level Grid Strategy

This strategy implements a symmetric grid trading system with up to three take profit ranks.
Limit orders are placed above and below the current price at fixed intervals. When an entry
order is filled, an opposite limit order is submitted to capture profit at a configurable
distance. The method is suitable for range-bound markets where price oscillates inside a band.

## Parameters

- `Grid Size` – distance between grid levels.
- `Levels` – number of grid levels on each side of the current price.
- `Base Take Profit` – base profit distance for the first rank.
- `Order Volume` – volume used for each grid order.
- `Enable Rank1` – place orders with base take profit.
- `Enable Rank2` – place orders with base plus one grid size take profit.
- `Enable Rank3` – place orders with base plus two grid sizes take profit.
- `Allow Longs` – enable the long side of the grid.
- `Allow Shorts` – enable the short side of the grid.
- `Candle Type` – candle type used to obtain the initial reference price.

## Trading Logic

1. On start the strategy subscribes to candles and waits for the first completed candle.
2. Using the close price of that candle the grid is built with the configured number of levels.
3. For each level buy and/or sell limit orders are placed depending on the allowed sides.
4. When an entry order is filled an opposite limit order is registered at the take profit price
   calculated from the selected rank.
5. Orders remain in the market until executed or cancelled manually.

This implementation is a simplified conversion from the original MQL grid system and aims to
highlight the core mechanics in StockSharp's high level API.
