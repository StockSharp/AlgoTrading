# Trailing Stop Strategy

## Overview
This strategy implements the trailing stop logic from the original MQL script `TRAILING.mq4`. It manages an existing open position and closes it when the market moves to a specified profit target or hits a stop loss. When the trailing parameter is enabled, the stop level follows the price to lock in profits.

## Parameters
- **TakeProfit** – profit distance from the entry price in absolute price units.
- **StopLoss** – maximum adverse distance allowed from the entry price.
- **Trailing** – distance used for dynamic trailing of the stop level.
- **CandleType** – candle series used to obtain price updates.

## How It Works
1. The strategy subscribes to the chosen candle series.
2. After each finished candle the current position is evaluated.
3. For long positions the strategy closes the position when profit exceeds *TakeProfit* or loss exceeds *StopLoss*.
4. If *Trailing* is greater than zero, the stop level moves up with the price. When price falls below the trailing stop the position is closed.
5. Short positions follow the same logic but in the opposite direction.
6. Entry price is recorded from the first executed trade and reset when the position is closed.

## Notes
- The strategy uses the high‑level API with `Bind` for processing candles.
- It does not open new positions by itself; it only manages an already opened position.
- Parameters are exposed via `StrategyParam` and can be optimized.
