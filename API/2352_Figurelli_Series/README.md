# Figurelli Series Strategy

## Overview
This strategy converts the MetaTrader5 expert "Exp_FigurelliSeries" to StockSharp. It uses a custom Figurelli Series indicator which measures the difference between the number of moving averages above and below the current price. Trading occurs once per day at a user-defined start time and all positions are closed at a stop time.

## Indicator
The Figurelli Series indicator creates a chain of exponential moving averages starting from *Start Period* and increasing by *Step* for *Total* averages. For each bar it counts how many averages are above and below the close price. The indicator value is `bids - asks` where `bids` is the count of averages below price and `asks` is the count of averages above price.

## Trading Rules
- At `Start Hour:Start Minute`:
  - Buy if the indicator value is positive and there is no long position.
  - Sell if the indicator value is negative and there is no short position.
- At or after `Stop Hour:Stop Minute`, any open position is closed.
- Only finished candles of the selected `Candle Type` are used.

## Parameters
- `StartPeriod` – initial moving average period.
- `Step` – period increment between averages.
- `Total` – number of moving averages.
- `StartHour` / `StartMinute` – time when entries may occur.
- `StopHour` / `StopMinute` – time to exit all positions.
- `CandleType` – candle type for calculations.
