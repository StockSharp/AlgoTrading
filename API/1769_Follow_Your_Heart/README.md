# Follow Your Heart Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader "Follow Your Heart" expert advisor. It analyses the last several candles and sums the relative changes of their open, close, high and low prices. A long position is opened when all changes are above a threshold and the combined value is positive. A short position is opened on the opposite conditions. Only one position can exist at a time.

Positions are protected by profit and loss limits measured in account currency and by take-profit/stop-loss in points. Optional trading sessions allow signals only inside specified hours.

## Parameters
- `Bars` – number of candles used to accumulate price changes. Default: 6.
- `Level` – threshold for open and close changes. Default: 2.3.
- `ProfitBuy` – money profit target to exit long position. Default: 75.
- `ProfitSell` – money profit target to exit short position. Default: 56.
- `LossBuy` – money loss threshold to exit long position. Default: -54.
- `LossSell` – money loss threshold to exit short position. Default: -51.
- `TakeProfit` – take profit in points. Default: 550.
- `StopLoss` – stop loss in points. Default: 550.
- `TradingHoursOn` – enable session filtering. Default: true.
- `OpenHourBuy` / `CloseHourBuy` – allowed hours for buy signals. Default: 6 / 12.
- `OpenHourSell` / `CloseHourSell` – allowed hours for sell signals. Default: 4 / 10.
- `CandleType` – candle timeframe. Default: 1 minute.

## Strategy Logic
1. For each finished candle compute the relative change of open, close, high and low compared with the previous candle and update moving sums.
2. If no position exists:
   - **Buy** when the total sum is positive, both open and close changes are above `Level`, and the close change is greater than the open change during buy session.
   - **Sell** when the total sum is negative, both open and close changes are below `-Level`, and the close change is less than the open change during sell session.
3. When a position exists, close it if profit or loss exceeds the configured money limits or if price moves by `TakeProfit`/`StopLoss` points.

## Notes
- Only market orders are used.
- Money management from the original code is simplified; position volume is taken from the strategy `Volume` property.
