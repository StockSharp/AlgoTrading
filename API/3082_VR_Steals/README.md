# VR STEALS Trailing Manager

## Origin
This strategy is a direct conversion of the **VR---STEALS** MetaTrader 5 expert advisor. The original script focused on money management around open positions and did not generate trading signals on its own. The StockSharp port keeps the same intent while adapting the mechanics to the high-level strategy API.

## Strategy Overview
VR STEALS Trailing Manager acts as a supervisory module that monitors already opened positions. It handles three risk control features:

1. **Fixed Stop Loss** – closes a position once the market moves against the entry by a configured price distance.
2. **Fixed Take Profit** – takes profit when price travels the specified distance in a favorable direction.
3. **Step-Based Trailing Stop** – after a trade becomes profitable, the stop level is moved in discrete steps, locking in more profit as the trend extends.

The strategy never opens trades on its own. Any entries must be created manually or by another automated component. This makes it a good companion for discretionary trading, signal followers, or as a plug-in for larger automated systems that outsource risk handling.

## Data Requirements
- Works on any security type supported by StockSharp.
- Subscribes to real-time trade data (`DataType.Ticks`) because the original MQL algorithm processed tick prices for immediate stop adjustments.
- The price distances are specified directly in absolute price units. When migrating from MetaTrader, multiply your pip values by the instrument's `PriceStep` to obtain comparable distances.

## Parameters
| Parameter | Description | Typical Use |
|-----------|-------------|-------------|
| `StopLossDistance` | Absolute loss distance. When the difference between the current price and the entry price exceeds this value against the position, a market exit is sent. Set to `0` to disable. | `0.0050` on EURUSD (≈ 50 pips with a 0.0001 step) |
| `TakeProfitDistance` | Absolute profit distance. Once the price advances at least this far in favor of the position, the strategy closes the trade to secure gains. Set to `0` to disable. | `0.0050` or larger depending on target |
| `TrailingStopDistance` | Distance maintained between price and the trailing stop. Used only when both the position is profitable and the trailing step condition is satisfied. | `0.0030` |
| `TrailingStepDistance` | Minimal incremental gain required before the trailing stop is moved closer to price. It prevents continuous updates on every minor fluctuation. Must be greater than zero when `TrailingStopDistance` is enabled. | `0.0005` |

All parameters can be optimized. During backtests, experiment with different combinations to match the characteristics of the underlying instrument.

## Execution Logic
1. **Stop-Loss Monitoring**
   - For long positions, if `entryPrice - lastPrice >= StopLossDistance`, a `SellMarket` order is sent.
   - For short positions, if `lastPrice - entryPrice >= StopLossDistance`, a `BuyMarket` order is sent.
2. **Take-Profit Monitoring**
   - Long trades close when `lastPrice - entryPrice >= TakeProfitDistance`.
   - Short trades close when `entryPrice - lastPrice >= TakeProfitDistance`.
3. **Trailing Stop Maintenance**
   - Activation requires `lastPrice` to move beyond `TrailingStopDistance + TrailingStepDistance` relative to the entry price.
   - The stop level is only advanced if it is at least `TrailingStepDistance` closer to price compared with the previously stored level.
   - Once price crosses the stored trailing stop (≤ for longs, ≥ for shorts), the position is closed.
4. **Order Throttling**
   - The strategy remembers when an exit order has been requested and avoids resending duplicates until the position size changes.

## Practical Usage Steps
1. Add the strategy to your StockSharp environment and select the security you plan to trade.
2. Configure the distance parameters according to your instrument's tick size and volatility. Remember to convert pip-based settings from MetaTrader into absolute prices.
3. Start the strategy before or immediately after entering trades. It will attach to the current position automatically; no extra configuration is required.
4. Monitor the log for validation. The strategy throws an exception at startup if `TrailingStopDistance > 0` while `TrailingStepDistance <= 0`, mirroring the safeguard from the MQL script.
5. Combine with other strategies or manual trading workflows as a risk control layer.

## Differences Compared to the MQL Version
- StockSharp does not modify broker-level stop orders. Instead, the strategy tracks the desired stop internally and closes positions via market orders when conditions are triggered.
- All logic runs on trade data rather than raw tick/ask/bid separation. This simplification maintains responsiveness while staying within StockSharp's recommended high-level API.
- Parameter names and units were adapted to StockSharp conventions (decimal price distances) instead of pip counts.
- Extensive comments and XML documentation were added to align with repository guidelines.

## Limitations and Notes
- The trailing functionality requires both distances to be positive; otherwise it remains inactive.
- Because exits are market orders, slippage depends on market liquidity and execution speed.
- When multiple positions share the same strategy instance, exits are generated for the aggregate position size reported by `Strategy.Position`.
- For backtesting accuracy, ensure the simulator feeds trade data with sufficient resolution; otherwise, exit triggers might be skipped.

## Recommended Extensions
- Pair the module with an entry strategy to create a fully automated workflow.
- Log the trailing stop values to a chart or storage for post-trade analysis.
- Add optional break-even logic if you need to secure capital once a minimum profit is reached.
