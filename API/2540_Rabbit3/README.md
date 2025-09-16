# Rabbit3 Strategy

## Overview
- Conversion of the original MetaTrader 5 expert advisor `Rabbit3 (barabashkakvn's edition)`.
- Implements the logic in the StockSharp high-level API with candle subscriptions and indicator bindings.
- Focuses on a dual confirmation between Williams %R and the Commodity Channel Index (CCI) before stacking positions.
- Adds dynamic position sizing: profits beyond a cash threshold increase the order volume for the next signal.

## Trading Logic
### Entry conditions
1. **Long**
   - Current and previous closed candles report Williams %R below `WilliamsOversold` (default `-80`).
   - CCI value is below `CciBuyLevel` (default `-80`).
   - Current net position is non-negative and adding another position keeps the exposure within `MaxPositions` × `BaseVolume` (boosted volume is used when active).
2. **Short**
   - Current and previous closed candles report Williams %R above `WilliamsOverbought` (default `-20`).
   - CCI value is above `CciSellLevel` (default `80`).
   - Current net position is non-positive and the new order remains within the configured stacking limit.

### Exit and risk control
- Protective stop-loss and take-profit orders are registered automatically through `StartProtection`.
- The distances are expressed in "adjusted points": when the instrument uses 3 or 5 decimals the strategy multiplies the price step by 10 to emulate MetaTrader pip arithmetic before applying `StopLossPips` and `TakeProfitPips`.
- No additional manual exit rules are required; protective orders close the trades.

### Position sizing
- `BaseVolume` defines the initial trade size (default `0.01`).
- After each trade closes, the realized PnL delta is compared with `ProfitThreshold` (default `4` monetary units).
- If the delta is strictly greater, the next signal uses `BaseVolume × VolumeMultiplier` (default `1.6`). Otherwise the size resets back to `BaseVolume`.
- The current volume is also exposed through the strategy `Volume` property for UI feedback.

### Indicators and visualization
- Williams %R, CCI, a fast EMA (`FastEmaPeriod`) and a slow EMA (`SlowEmaPeriod`) are bound to the candle feed for monitoring and charting.
- A chart area is created automatically, plotting candles, indicators and executed trades.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | `1h` time frame | Data type for candle subscription. |
| `CciPeriod` | `15` | Length of the Commodity Channel Index. |
| `CciBuyLevel` | `-80` | CCI threshold that allows long entries. |
| `CciSellLevel` | `80` | CCI threshold that allows short entries. |
| `WilliamsPeriod` | `62` | Lookback period for Williams %R. |
| `WilliamsOversold` | `-80` | Oversold threshold used for long confirmation. |
| `WilliamsOverbought` | `-20` | Overbought threshold used for short confirmation. |
| `FastEmaPeriod` | `17` | Fast EMA plotted for trend context. |
| `SlowEmaPeriod` | `30` | Slow EMA plotted for trend context. |
| `MaxPositions` | `2` | Maximum number of stacked entries per direction. |
| `ProfitThreshold` | `4` | Realized profit required to boost the next order size (monetary units). |
| `BaseVolume` | `0.01` | Base order volume. |
| `VolumeMultiplier` | `1.6` | Multiplier applied when the boost condition is met. |
| `StopLossPips` | `45` | Stop-loss distance in adjusted points. |
| `TakeProfitPips` | `110` | Take-profit distance in adjusted points. |

## Notes
- The strategy operates on net positions. Unlike the hedging-friendly MQL version, longs and shorts are not held simultaneously; signals in the opposite direction are ignored until the current exposure is closed by protective orders.
- `MaxPositions` works as a cap on the aggregate position (base volume multiplied by the stacking factor). Adjust it carefully when changing the base or boosted volumes.
- Volume tolerance uses half of the instrument volume step to absorb minor rounding differences when checking the stacking cap.
- Python translation is not included yet and can be added later if needed.
