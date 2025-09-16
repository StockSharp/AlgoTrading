# N Candles v3 Strategy

## Overview
This strategy scans the latest finished candles and looks for a sequence where the last *N* bars share the same direction (all bullish or all bearish). When such a streak appears it enters in the direction of the sequence while respecting a cap on how many positions can be opened at once. The implementation migrates the original MetaTrader 5 expert advisor to the StockSharp high level API.

## Trading Logic
- The engine subscribes to the configured candle type and processes only completed bars.
- For every finished candle the body direction is evaluated: bullish, bearish, or neutral (doji).
- Doji candles reset the internal counter. Otherwise the counter increases when the current candle has the same direction as the previous ones. Once the counter reaches the `Identical Candles` parameter the strategy issues a new order.
- Long signals close any existing short exposure first and then add a long unit while the total bought volume stays below `Max Positions * Volume`.
- Short signals work symmetrically for bearish streaks.

## Risk Management
- After each filled trade the strategy places new protective stop-loss and take-profit orders based on the average entry price of the active position.
- Distances are measured in security price steps: `Take Profit Points` multiplies the step to calculate the target above (long) or below (short) the entry; `Stop Loss Points` uses the same idea for the protective stop.
- A stepped trailing stop can replace the initial stop once price moves by `Trailing Stop Points` in favor of the position. The stop is moved only when the price has advanced by at least `Trailing Step Points` beyond the previous trailing level.

## Parameters
- **Candle Type** – Time frame or candle source to analyse.
- **Identical Candles** – Required number of consecutive candles with the same direction to trigger an entry.
- **Volume** – Order size for each new entry in security units.
- **Max Positions** – Maximum number of entry units that may be open in the same direction simultaneously.
- **Take Profit Points** – Take-profit distance in multiples of the instrument price step.
- **Stop Loss Points** – Stop-loss distance in multiples of the instrument price step.
- **Trailing Stop Points** – Distance from the current price used to activate and maintain the trailing stop. Set to zero to disable trailing.
- **Trailing Step Points** – Extra distance in price steps that must be covered before the trailing stop is moved again.

## Additional Notes
- The strategy operates in a netting manner: when a signal in the opposite direction appears, any existing exposure on the other side is closed before adding a new position.
- All protective orders are re-created after every fill to keep their volume synchronized with the open position size.
- Make sure the instrument provides a non-zero `PriceStep`; otherwise the default step value of 1 is used.
