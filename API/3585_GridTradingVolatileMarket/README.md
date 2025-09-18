# Grid Trading at Volatile Market

## Overview
This strategy replicates the MetaTrader expert "Gridtrading_at_volatile_market.mq4" using the StockSharp high-level API. It trades around Donchian channel boundaries detected on a higher timeframe while confirming entries with engulfing patterns on the trading timeframe. Once a grid is active the strategy adds averaging orders when price extends by multiples of the higher timeframe ATR and exits when portfolio profit or drawdown targets are reached.

## How It Works
1. Two candle streams are used: the user-selected trading timeframe and a higher timeframe automatically derived from it (M1→M5→M15→M30→H1→H4→D1).
2. On the higher timeframe the strategy calculates:
   - `ATR(20)` to size grid spacing.
   - `SMA(SlowMaLength)` to filter the trend together with RSI.
   - `DonchianChannels(20)` for support and resistance levels.
3. On the trading timeframe it tracks the last two completed candles to detect bullish or bearish engulfing patterns.
4. A long grid starts when the previous candle touches the Donchian lower band, forms a bullish engulfing pattern, and RSI confirms oversold conditions (`RSI < 35` while price is above the higher timeframe SMA). A short grid mirrors these rules at the upper band with `RSI > 65`.
5. After the first market order the strategy keeps the initial price as an anchor. If price moves against the position by `2 * ATR` for the current grid step it adds another order with volume multiplied by `GridMultiplier`.
6. The grid is closed and all orders are cancelled when either:
   - The combined (realised + unrealised) PnL exceeds `TakeProfitFactor * total grid volume`.
   - Drawdown falls below `-MaxDrawdownFraction * initial portfolio value`.

## Parameters
- **TakeProfitFactor** – profit multiple of the total grid volume required to close the grid (default `0.1`).
- **SlowMaLength** – period of the higher timeframe SMA used for filtering (default `50`).
- **GridMultiplier** – geometric factor applied to each additional averaging order (default `1.5`).
- **BaseOrderVolume** – volume of the first order in the grid (default `0.1`).
- **MaxDrawdownFraction** – maximum loss relative to initial portfolio value before the grid is force-closed (default `0.8`).
- **CandleType** – trading timeframe. The higher timeframe is inferred automatically.

## Notes
- Only closed candles are processed to avoid repainting.
- The strategy relies on available bid/ask quotes to evaluate open PnL; if only last trade prices are provided the approximation may be less accurate.
- When the portfolio information is not available the drawdown protection is skipped, allowing the grid to run until the profit target is met or the position is closed manually.
