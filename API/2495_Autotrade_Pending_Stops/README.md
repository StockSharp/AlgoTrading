# Autotrade Pending Stops Strategy

## Overview
This strategy is a C# conversion of the MetaTrader expert advisor *Autotrade (barabashkakvn's edition)*. It continuously maintains two symmetric stop entry orders around the current market price. Whenever the market remains flat and no position is open, the strategy refreshes both pending orders. When a stop order fills, the position is actively monitored: exits are triggered once the price action stabilizes or when an absolute profit/loss threshold is met. The implementation uses the high-level StockSharp API as required by the project guidelines.

## Mapping from the original inputs
| StockSharp parameter | MQL5 input | Description |
| --- | --- | --- |
| `IndentTicks` | `InpIndent` | Distance (in price steps) between the current price and the stop entry orders. |
| `MinProfit` | `MinProfit` | Minimum floating profit (account currency) needed to exit during a quiet market phase. |
| `ExpirationMinutes` | `ExpirationMinutes` | Lifetime of the pending stop orders before they are cancelled and recreated. |
| `AbsoluteFixation` | `AbsoluteFixation` | Absolute profit or loss level (currency) that forces the position to close. |
| `StabilizationTicks` | `InpStabilization` | Maximum size of the previous candle body that is treated as a consolidation zone. |
| `OrderVolume` | `Lots` | Volume used for both the buy stop and the sell stop orders. |
| `CandleType` | `Period()` | Candle series that drives the logic (default 1-minute time frame). |

All numerical inputs that represent price distances are converted from "points" to actual price steps through the `Security.PriceStep` value. Profit-based thresholds are calculated using `Security.StepPrice`, which mirrors the MQL profit calculations that operate in the deposit currency.

## Trading logic
### Pending order deployment
1. The strategy reacts only to finished candles (`CandleStates.Finished`).
2. The very first candle is used to seed historical data (previous open/close) and immediately schedule pending orders.
3. When no position is open, any inactive references are cleared and:
   - A buy stop is placed at `Close + IndentTicks * PriceStep`.
   - A sell stop is placed at `Close - IndentTicks * PriceStep`.
4. Each pending order receives an expiration timestamp equal to `CloseTime + ExpirationMinutes` minutes. When that time is reached the order is cancelled and recreated on the next candle.

### Position management
1. Once either stop order is executed, the opposite pending order is cancelled to avoid unwanted hedging on the netting-based StockSharp account model.
2. The strategy keeps the previous candle body (`|Open - Close|`) to detect quiet market conditions.
3. For every candle with an open position:
   - Unrealized profit is estimated in currency using the price difference versus `PositionAvgPrice`, scaled by `Security.PriceStep` and `Security.StepPrice`.
   - If the profit exceeds `MinProfit` **and** the previous candle body is below `StabilizationTicks * PriceStep`, the position is closed at market.
   - Regardless of stabilization, if the absolute profit or loss exceeds `AbsoluteFixation`, the position is also closed at market.
4. Whenever the position returns to flat, all remaining pending orders are cleared.

### Additional behaviors
- Only one position is allowed at a time; order volumes are netted using `OrderVolume`.
- Because StockSharp does not expose bid/ask during backtests in the same way as MetaTrader, the close price of the completed candle is used as the reference level for new stop orders.
- The strategy automatically refreshes the cached `Volume` value whenever `OrderVolume` is adjusted via parameters or optimization.

## Implementation notes and differences
- Profit calculations rely on `Security.PriceStep` and `Security.StepPrice`. Ensure these fields are filled in the instrument metadata; otherwise the default value `1` is used as a fallback.
- The original MQL version allowed temporary hedging (multiple orders in opposite directions). The StockSharp port cancels the unused stop immediately after a fill to comply with the platform's netting model.
- Pending order expiration uses the candle's `CloseTime`. If historical data lacks close timestamps, adjust the feed to provide them or extend the code accordingly.
- The strategy works with any candle data type by adjusting `CandleType`. Default candles are time-frame based (`TimeSpan.FromMinutes(1).TimeFrame()`).

## Usage recommendations
1. Configure the candle series that matches the chart period used in MetaTrader.
2. Set `IndentTicks`, `StabilizationTicks`, and profit thresholds in relation to the instrument's tick size and tick value.
3. Verify that the portfolio uses hedging or netting as desired. The strategy assumes netting and will flat the book before rearming stop orders.
4. Use the provided parameters for optimization in StockSharp Designer or Backtester to adapt the behaviour to different markets.
5. Monitor the log output: the code relies on finished candles and market availability (`IsFormedAndOnlineAndAllowTrading()`) before it submits new orders.

## Risk disclaimer
Automated trading involves substantial risk. Backtest thoroughly, validate the parameters on historical data, and confirm broker-specific requirements (such as minimum distances for stop orders) before deploying the strategy on a live account.
