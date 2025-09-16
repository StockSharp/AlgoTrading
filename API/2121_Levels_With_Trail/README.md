# Levels With Trail Strategy

Converted from MQL script `levels_with_trail.mq4`. The strategy opens trades when price crosses a specified level and can trail the stop loss.

## How it works
- Subscribes to candles of the chosen timeframe.
- When there is no open position and the closing price is above `Level Price`, it buys; if the price is below, it sells.
- If `Trail Stop` is enabled, the stop loss follows the price when the position is profitable.
- Positions are closed when the stop loss, take profit, or an opposite breakout signal is triggered.

## Parameters
- `Stop Loss` – stop loss size in price units.
- `Take Profit` – take profit size in price units.
- `Level Price` – breakout level to watch.
- `Trail Stop` – enable or disable the trailing stop loss.
- `Candle Type` – candle timeframe used for analysis.
