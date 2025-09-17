# FT CCI MA (StockSharp port)

## Overview
This strategy is a direct port of the MetaTrader expert "FT CCI MA". It trades the close of each finished candle, combining a linear weighted moving average (LWMA) with Commodity Channel Index (CCI) thresholds and an optional trading session filter. The StockSharp implementation keeps the same parameter names and default values, allowing you to reproduce the original behaviour while benefiting from the high-level API (candle subscriptions, indicator binding, position protection).

Key design notes:
- The LWMA works on the weighted price `(High + Low + 2 * Close) / 4`, matching the `PRICE_WEIGHTED` mode from MetaTrader.
- The CCI uses the typical price `(High + Low + Close) / 3`, as in `PRICE_TYPICAL`.
- All decisions are evaluated on the just closed bar, which mirrors the original EA that waited for the start of the next bar before acting on the previous one.
- Position protection replicates the EA's take-profit and stop-loss in pip units.

## Trade rules
1. **Long entries**
   - Close price above the LWMA and CCI below `CciLevelBuy` (default -100), *or*
   - Close price below the LWMA and CCI below `CciLevelDown` (default -200).
   - Enter only if the current net position is flat or short.
2. **Short entries**
   - Close price below the LWMA and CCI above `CciLevelSell` (default 100), *or*
   - Close price above the LWMA and CCI above `CciLevelUp` (default 200).
   - Enter only if the current net position is flat or long.
3. **Time filter**
   - When `UseTimeFilter` is enabled the strategy checks the hour of `candle.CloseTime`.
   - If the hour is outside the active window, all positions and orders are cancelled/closed immediately.
4. **Risk controls**
   - `StartProtection` sets absolute stop-loss and take-profit distances using pip size derived from `Security.PriceStep`.
   - Order volume is netted so opening in the opposite direction automatically closes the previous exposure.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `OrderVolume` | Trade size in lots. | `1` |
| `StopLossPips` | Stop-loss distance expressed in pips (0 disables). | `150` |
| `TakeProfitPips` | Take-profit distance in pips (0 disables). | `150` |
| `UseTimeFilter` | Enables the session filter. | `true` |
| `StartHour` | Session start hour in exchange time (0-23). | `10` |
| `EndHour` | Session end hour in exchange time (0-23). When lower than the start hour the session spans midnight. | `5` |
| `CciPeriod` | Commodity Channel Index length. | `14` |
| `CciLevelUp` | Aggressive short threshold (+200). | `200` |
| `CciLevelDown` | Aggressive long threshold (-200). | `-200` |
| `CciLevelBuy` | Soft long threshold when price is above the MA (-100). | `-100` |
| `CciLevelSell` | Soft short threshold when price is below the MA (+100). | `100` |
| `MaPeriod` | LWMA length. | `200` |
| `MaShift` | Horizontal shift of the LWMA in bars. The current candle compares against the value `MaShift` bars back. | `0` |
| `CandleType` | Candle data type/time frame used for calculations. | `1 hour time frame` |

## Implementation details
- **Pip calculation** – Pip size equals `Security.PriceStep`. For 3 or 5 decimal forex symbols it is multiplied by 10 to translate 0.00001 into the 0.0001 pip used by the EA.
- **Session filter** – Implements the two scenarios from the MQL source: intraday windows (`StartHour < EndHour`) and overnight windows (`StartHour > EndHour`). When `StartHour == EndHour` trading is disabled, matching the original logic.
- **Indicator binding** – Uses `SubscribeCandles().Bind(...)` so the CCI and LWMA receive automatic updates without manual buffering. Values are stored only to support the optional LWMA shift, avoiding direct calls to `GetValue()`.
- **Order management** – `CancelActiveOrders()` runs before each market order, mirroring the EA's behaviour of keeping a clean order book.
- **No Python version** – Only the C# strategy is provided, as requested.

## Usage
1. Attach the strategy to a security and set `CandleType` to the desired timeframe.
2. Choose volume and pip parameters appropriate for the instrument (remember to align broker pip definitions with the built-in conversion).
3. Enable or disable the session filter according to your trading hours.
4. Start the strategy; it will subscribe to candles, apply indicator logic, and manage orders/stops automatically.

