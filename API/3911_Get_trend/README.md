# Get trend Strategy

The strategy is a StockSharp high-level port of the MetaTrader 4 expert advisor **Get trend.mq4**. It evaluates the M15 chart for
entries, validates the broader trend on H1, and relies on two smoothed moving averages together with a pair of stochastic
oscillators to detect mean-reversion breakouts near the longer-term trend. The implementation keeps the original money management
rules based on fixed take-profit, stop-loss, and trailing-stop distances expressed in price points.

## Trading logic

1. **Indicators and data**
   - M15 candles feed a smoothed moving average (SMMA, median price) with period `M15MaPeriod` and two stochastic oscillators.
   - H1 candles feed another SMMA (median price) with period `H1MaPeriod`.
   - The fast stochastic (`FastStochasticPeriod`, 3, 3) supplies the %K line and its previous value. The slow stochastic (`SlowStochasticPeriod`, 3, 3) supplies the %D signal line.
2. **Long setup**
   - The current M15 close is below its SMMA and the H1 close is below its own SMMA.
   - The distance between the M15 SMMA and the close is within `ThresholdPoints` price steps.
   - Both stochastic lines are below 20. The fast line crosses above the slow line during the last candle (`fast` > `slow` while the previous fast value was below `slow`).
   - If a short position exists, the strategy first buys enough volume to flatten it and then opens a new long with `TradeVolume`.
3. **Short setup** mirrors the long logic:
   - Both closes lie above their SMMAs, the distance is within `ThresholdPoints`, the stochastic values are above 80, and the fast
     line crosses below the slow line. The strategy sells, closing an existing long if necessary.
4. **Risk management**
   - After every entry, protective orders are placed at `StopLossPoints` and `TakeProfitPoints` (converted into absolute price
     distances using the instrument's price step).
   - A trailing stop re-aligns the stop-loss order once the trade gains at least `TrailingStopPoints` points. The new stop is
     positioned at the current close minus/plus the trailing distance for longs/shorts respectively.
   - When the position returns to flat, all protective orders are cancelled.

## Differences versus the original EA

- MetaTrader's SMMA uses an indicator shift of eight bars; StockSharp indicators do not expose a direct shift setting. The port
  evaluates the most recent finished value instead. This keeps the crossover timing while avoiding additional custom buffers.
- The original EA used MQL's bid/ask quotes for trailing. The port uses the finished candle close that triggered the trailing
  update, which is the closest analogue available in the high-level API.
- Money management relies on StockSharp's order registration helpers (`BuyMarket`, `SellMarket`, `SellStop`, etc.) instead of
  `OrderSend` and `OrderModify`.

## Parameters

| Group | Name | Description | Default |
|-------|------|-------------|---------|
| Data | `M15 Candle Type` | Candle type/timeframe used for the main calculations. | M15 time frame |
| Data | `H1 Candle Type` | Candle type/timeframe used for confirmation. | H1 time frame |
| Indicators | `M15 SMMA Period` | Length of the smoothed moving average on the M15 series. | 200 |
| Indicators | `H1 SMMA Period` | Length of the smoothed moving average on the H1 series. | 200 |
| Indicators | `Slow Stochastic Period` | %K length for the slow stochastic oscillator that provides the %D line. | 14 |
| Indicators | `Fast Stochastic Period` | %K length for the fast stochastic oscillator that provides the main %K line. | 14 |
| Signals | `Threshold (points)` | Maximum distance between the M15 SMMA and the current close to allow entries. | 50 |
| Risk | `Take Profit (points)` | Take-profit distance expressed in price steps. | 570 |
| Risk | `Stop Loss (points)` | Stop-loss distance expressed in price steps. | 30 |
| Risk | `Trailing Stop (points)` | Trailing-stop distance expressed in price steps. | 200 |
| Trading | `Trade Volume` | Volume sent with each market order. | 0.1 |

## Notes for usage

- Ensure the traded security exposes `PriceStep`; otherwise the point-based distances fall back to `1`, which can lead to large
  protective orders on instruments quoted in fractional units.
- The strategy cancels and recreates stop orders as soon as a better trailing level is detected. Brokers that disallow frequent
  modifications might require throttling.
- Because the port operates on finished candles only, the system is designed for backtests and end-of-bar execution. Running it on
  live tick data requires matching the candle building settings between the terminal and StockSharp.
