# FiftyFiveMedianSlopeStrategy

## Origin
- Converted from the MetaTrader 4 expert advisor **55_MA_med_FIN.mq4**.
- Focuses on the slope of a 55-period moving average calculated on median candle prices.

## Trading logic
- Subscribes to the configured candle series (default: 1-hour time frame) and processes only completed candles.
- Calculates a moving average on the median price (\((High + Low) / 2\)) using the selected method (SMA, EMA, SMMA or LWMA).
- Stores the latest moving average values in a circular buffer to compare the value one bar ago with the value `MaShift` bars ago.
- When the value from one bar ago is greater than the value from `MaShift` bars ago, the strategy:
  - Closes any short exposure first.
  - Opens a long position if the `MaxOrders` limit has not been reached.
- When the value from one bar ago is less than the value from `MaShift` bars ago, it mirrors the behaviour for short positions.
- Signals are alternated via internal flags so the strategy waits for an opposite crossover before re-entering in the same direction.
- Trading is allowed only while the candle opening hour satisfies `StartHour < hour < EndHour`. The bounds are exclusive to match the original MQL implementation.

## Position sizing and risk management
- `FixedVolume` defines the lot size per market order. When set to zero, the strategy switches to risk-based sizing using `RiskPercentage` and the portfolio's current value.
- `MaxOrders` limits how many times the base volume can be stacked in the same direction. A value of zero removes the cap.
- Optional `StopLossPoints` and `TakeProfitPoints` recreate the MT4 stop-loss and take-profit distances via `StartProtection` using price steps.

## Parameters
- `FixedVolume` – primary lot size. Set to zero to enable percentage-based sizing.
- `RiskPercentage` – fraction of the portfolio allocated when `FixedVolume` equals zero.
- `TakeProfitPoints` / `StopLossPoints` – protective distances expressed in price steps.
- `MaPeriod` – length of the median moving average (default 55).
- `MaShift` – number of bars between the recent and historical moving average snapshots (default 13).
- `MaMethod` – moving average calculation type (Simple, Exponential, Smoothed, LinearWeighted).
- `StartHour` / `EndHour` – exclusive trading window in platform time (0–23 hours).
- `MaxOrders` – maximum simultaneous entries per direction.
- `CandleType` – time frame used for the signal candles.

## Usage notes
- Ensure the subscribed instrument provides a non-zero `PriceStep` and volume metadata so the volume alignment matches exchange requirements.
- Risk-based sizing uses the portfolio current value and the latest close price. If either is unavailable the strategy falls back to zero volume (no trade).
- The strategy cancels opposite exposure before opening a new position, emulating the original MT4 behaviour of closing opposing orders.
