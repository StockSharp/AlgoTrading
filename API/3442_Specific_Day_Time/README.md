# Specific Day & Time Orders

This strategy replicates the MetaTrader expert *"Expert Advisor specific day and time"*.  
It places buy and/or sell orders at a scheduled timestamp and optionally removes every exposure at another timestamp.  
The StockSharp version keeps the original risk-management behaviour, including optional trailing stops and break-even moves.

## Core logic

1. **Scheduling**  
   - `OpenTime` – moment when orders are created.  
   - `CloseTime` – moment when positions are flattened and pending orders can be removed.  
   Both checks accept a one-minute window, matching the `TimeToString(..., TIME_MINUTES)` comparison used in the MT4 code.

2. **Order placement**  
   - `OrderPlacement` chooses between market, stop, or limit orders.  
   - `OpenBuyOrders` / `OpenSellOrders` enable the desired directions.  
   - `OrderDistancePoints` offsets the price of pending orders by a number of points (pips).  
   - `PendingExpireMinutes` cancels pending orders automatically when their validity window ends.

3. **Volume management**  
   - `LotSizing = Manual` sends the fixed `ManualVolume`.  
   - `LotSizing = Automatic` calculates volume from the current portfolio value and the instrument contract size:  
     `volume = (portfolio / contractSize) * RiskFactor`.  
   The result is aligned to `Security.VolumeStep` and clamped between `MinVolume`/`MaxVolume` when available.

4. **Protection logic**  
   - `StopLossPoints` and `TakeProfitPoints` translate the original point-based distances to absolute prices using the instrument pip size.  
   - `TrailingStopEnabled` + `TrailingStepPoints` and `BreakEvenEnabled` move the protective stop exactly like the MQL script, using bid/ask updates as triggers.  
   - When stop-loss or take-profit conditions are hit, the position is closed with a market order, mirroring the MT4 behaviour of modifying stops to a new price.

5. **Closing phase**  
   - When `CloseOwnOrders` or `CloseAllOrders` is enabled the strategy exits any open position in the close window.  
   - `DeletePendingOrders` removes all remaining pending orders at the same time.

## Parameters

| Name | Description |
|------|-------------|
| `OpenTime`, `CloseTime` | UTC timestamps for entering and exiting the market. |
| `OrderPlacement` | Market, stop, or limit order placement. |
| `OpenBuyOrders`, `OpenSellOrders` | Directions to activate. |
| `TakeProfitPoints`, `StopLossPoints` | Protective distances expressed in points (0 disables). |
| `TrailingStopEnabled`, `TrailingStepPoints` | Enable trailing stop and define the minimum advance before moving it. |
| `BreakEvenEnabled`, `BreakEvenAfterPoints` | Shift the stop to break-even once profit exceeds the threshold. |
| `OrderDistancePoints` | Offset used for pending orders. |
| `PendingExpireMinutes` | Expiration window for pending orders. |
| `LotSizing` | Manual or automatic volume sizing. |
| `RiskFactor`, `ManualVolume` | Inputs for the sizing modes. |
| `CloseOwnOrders`, `CloseAllOrders`, `DeletePendingOrders` | Control how positions and pending orders are closed at the end. |

## Notes

- The class lives in the `StockSharp.Samples.Strategies` namespace with tab indentation as required by the project guidelines.  
- Level1 data is used to reproduce bid/ask-sensitive logic from the MQL version (trailing stop, pending order placement).  
- `MagicNumber` settings from MT4 are not required because StockSharp already isolates strategy orders.

## Usage

1. Compile the project via `AlgoTrading.sln` and attach the strategy to a security/portfolio pair.  
2. Adjust the schedule, order type, and risk parameters as needed.  
3. Start the strategy before `OpenTime`; orders will be sent automatically once the window begins.  
4. Keep the strategy running until after `CloseTime` if you want the automatic flattening step to fire.
