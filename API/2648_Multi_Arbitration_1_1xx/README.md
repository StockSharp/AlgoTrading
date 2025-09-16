# Multi Arbitration 1.1xx Strategy

## Overview
This strategy replicates the logic of the MetaTrader "Multi Arbitration 1.1xx" expert by managing two correlated instruments through a breakout-style averaging process. The StockSharp implementation works with the primary `Security` assigned to the strategy and a configurable second instrument. On each finished candle (same timeframe for both securities) it compares current prices with the lowest filled buy and highest filled sell levels and reacts by stacking additional positions when new extremes appear.

## Parameters
- **Signal Candles (`CandleType`)** – timeframe used to build candles for both instruments (default 15 minutes). The chart timeframe in StockSharp can differ because signal generation relies solely on the subscription created by the strategy.
- **Second Security (`SecondSecurity`)** – instrument traded alongside the main security. It must be specified before starting the strategy.
- **Profit Target (`ProfitTarget`)** – account-level profit (Portfolio current value minus initial value) that triggers liquidation of all positions.
- **Max Open Units (`MaxOpenUnits`)** – limit on the combined absolute exposure across both instruments. If this limit is reached and the portfolio shows profit, the strategy exits everything.
- **Second Volume (`SecondVolume`)** – order size used for the secondary security. The main security uses the standard `Strategy.Volume` value.

## Trading Logic
1. Subscribes to the configured candle type for the main security and the secondary instrument. Processing starts only when both series deliver a finished candle with the same open time.
2. Maintains the most favorable filled price on each side (lowest buy, highest sell) for both instruments. When the current close crosses a new extreme, the strategy adds to the corresponding position:
   - Price breaks below the stored buy level → add a long position (or initialise one if flat).
   - Price breaks above the stored sell level → add a short position (or initialise one if flat).
3. The algorithm is net-position based. When no position exists it defaults to opening a long on the next evaluation to keep the grid active.
4. Before every decision the total exposure (`|pos1| + |pos2|`) is checked against the `MaxOpenUnits` limit. Surpassing the limit with a profitable portfolio forces an exit to reduce risk.
5. Whenever the accumulated profit exceeds `ProfitTarget`, the strategy closes all positions on both instruments and resets the internal breakout levels.

## Differences from the Original MQL Expert
- StockSharp accounts are netting by default, therefore simultaneous long and short hedged positions (used in MetaTrader via `PositionCloseBy`) are approximated with net exposure management.
- Minimum lot discovery and asynchronous order retries from the MQL version are simplified to rely on the platform order handling. Each instrument uses a constant volume (`Volume` for the main security, `SecondVolume` for the second one).
- Platform permission checks (`IsTradeAllowed`, filling policy detection) are handled by StockSharp infrastructure and are not duplicated in code.

## Usage Notes
- Ensure the `SecondSecurity` parameter is filled and the data connector supplies quotes for both instruments before starting the strategy.
- Adjust `Volume` and `SecondVolume` to match the minimum tradable size of each instrument.
- Consider running the strategy on connectors that support synchronized timestamps for both markets, because trading logic waits for identical candle open times before acting.
