# RobotPower M5 Strategy

This strategy combines the Bulls Power and Bears Power indicators on a 5-minute chart.
It opens positions when the combined momentum of bulls and bears crosses zero and manages exits with fixed targets and a trailing stop.

## How It Works
- **Indicators**: Bulls Power and Bears Power with a shared period `BullBearPeriod`.
- **Timeframe**: 5-minute candles by default (`CandleType`).

### Entry Rules
- **Long Entry**: When `BullsPower + BearsPower > 0` and no position is open, buy at market.
- **Short Entry**: When `BullsPower + BearsPower < 0` and no position is open, sell at market.

### Exit Rules
- **Take Profit**: Close the position when price moves `TakeProfit` units in the trade direction.
- **Stop Loss**: Close the position if price moves against the position by `StopLoss` units.
- **Trailing Stop**: After entry, the stop loss trails by `TrailingStep` once price advances more than twice that distance.

### Parameters
- `BullBearPeriod` – period for both Bulls Power and Bears Power calculations.
- `TrailingStep` – step size used when adjusting the trailing stop.
- `TakeProfit` – distance from entry to the take-profit level.
- `StopLoss` – distance from entry to the stop-loss level.
- `CandleType` – candle timeframe for signal calculation.

### Position Size
Uses the strategy's `Volume` property for order size.

## Notes
Designed for educational purposes and serves as an example of converting an MQL strategy to the StockSharp API.
