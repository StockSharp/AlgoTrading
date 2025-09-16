# Two MA RSI Strategy

## Overview
Two MA RSI Strategy is a conversion of the original MetaTrader "2MA_RSI" expert advisor. It uses a fast and a slow exponential moving average (EMA) crossover confirmed by a Relative Strength Index (RSI) filter. Orders are sized with a martingale-style money management block that increases the next order volume after a loss. The StockSharp version works entirely on finished candles and reproduces the original take-profit and stop-loss behaviour in price points.

## Data and indicators
- The strategy subscribes to a single candle series defined by `CandleType` (5-minute candles by default).
- Three indicators are calculated on every completed bar:
  - `FastLength` EMA (applied to the candle close).
  - `SlowLength` EMA.
  - RSI with length `RsiLength`.
- Historical indicator values are stored internally to detect EMA crossovers without pulling data from indicator buffers.

## Entry logic
1. The previous candle must be finished to avoid intrabar re-evaluation.
2. No active position is allowed (`Position == 0`).
3. **Long entry:**
   - The fast EMA crosses above the slow EMA (`fast EMA` on the current bar is greater than the slow EMA while the previous bar had `fast EMA < slow EMA`).
   - The RSI value is below `RsiOversold`, confirming an oversold market.
4. **Short entry:**
   - The fast EMA crosses below the slow EMA with the analogous condition (`fast EMA` now below `slow EMA`, previously above).
   - RSI is above `RsiOverbought`, signalling an overbought market.
5. When all conditions are satisfied the strategy sends a market order sized according to the martingale module.

## Exit logic
- A protective stop loss and a take profit are calculated immediately after each entry. Distances are defined in "points" and converted through the instrument `PriceStep`:
  - **Long:**
    - Stop loss = `entry price - StopLossPoints * PriceStep`.
    - Take profit = `entry price + TakeProfitPoints * PriceStep`.
  - **Short:**
    - Stop loss = `entry price + StopLossPoints * PriceStep`.
    - Take profit = `entry price - TakeProfitPoints * PriceStep`.
- Only these protective levels close a trade. The strategy waits for the next candle to confirm whether the low/high touched the target or stop and sends a market `ClosePosition()` order accordingly.
- Exit priority matches the conservative behaviour of the original robot: a stop loss is evaluated before a take profit if both levels fall inside the same candle range.

## Position sizing and martingale
1. The base volume is calculated on every entry as `floor(balance / BalanceDivider) * VolumeStep`. The value always stays at or above one volume step and uses portfolio `CurrentValue` (falling back to `BeginValue` when necessary).
2. After each losing exit the martingale stage increases by one up to `MaxDoublings`. The next order volume is multiplied by `2^stage`.
3. Any winning trade or reaching the maximum number of doublings resets the stage to zero, returning to the base volume.
4. If `MaxDoublings` is zero or negative the size never increases and equals the base volume.

## Additional behaviour
- The strategy keeps track of previous EMA values internally and does not request historical indicator values.
- Orders are executed only when the strategy is online, indicators are formed, and trading is allowed.
- Chart output draws price candles, own trades, and the three indicators for visual analysis.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `FastLength` | Length of the fast EMA. | 5 |
| `SlowLength` | Length of the slow EMA. | 20 |
| `RsiLength` | Number of bars used in RSI calculation. | 14 |
| `RsiOverbought` | RSI level that blocks new longs and allows shorts. | 70 |
| `RsiOversold` | RSI level that allows longs. | 30 |
| `StopLossPoints` | Stop loss distance expressed in price steps. | 500 |
| `TakeProfitPoints` | Take profit distance in price steps. | 1500 |
| `BalanceDivider` | Divides portfolio value to obtain the base order size. | 1000 |
| `MaxDoublings` | Maximum number of martingale doublings after consecutive losses. | 1 |
| `CandleType` | Candle series used by the strategy. | 5-minute timeframe |

## Usage notes
- Provide a portfolio and security with valid `PriceStep` and `VolumeStep` metadata so that point-based risk management and position sizing remain consistent.
- Because market orders are used for exits, slippage and spreads are still possible compared with the limit orders of the MetaTrader version, but the logic of stop/take evaluation is preserved.
- The strategy does not create a Python version; only the C# implementation is supplied as requested.
