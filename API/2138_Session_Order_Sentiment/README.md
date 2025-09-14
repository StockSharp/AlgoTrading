# Session Order Sentiment Strategy

## Overview
This strategy trades based on the imbalance between buy and sell orders observed in the order book. It measures ratios of order counts and total volumes for both sides of the book and opens a position when the dominance of one side exceeds configurable thresholds. Trading is allowed only during a specified time window.

After a position is opened, the thresholds are reduced to monitor the opposite side. If the opposite side grows beyond these reduced thresholds, the position is closed. A stop loss and take profit are also applied using absolute price points.

## Trading Rules
- **Long Entry**: Buy when
  - `BUY volume / SELL volume >= DiffVolumesEx` and `BUY orders / SELL orders >= DiffTradersEx`
  - Either side meets `MinTraders` and `MinVolume`
  - Current time passes `CheckTradingTime`
- **Short Entry**: Sell when the above logic is mirrored for the sell side.
- **Exit**:
  - Close the long when `SELL volume / BUY volume > 1 / DiffVolumes` or `SELL orders / BUY orders > 1 / DiffTraders`
  - Close the short when `SELL volume / BUY volume < DiffVolumes` or `SELL orders / BUY orders < DiffTraders`
  - Close all positions outside of trading hours
- **Stops**: Uses `Stop Loss` and `Take Profit` measured in price points.

## Parameters
- `MinVolume` – minimum total volume required on either side of the book (default: 20000)
- `MinTraders` – minimum number of orders on either side (default: 1000)
- `DiffVolumesEx` – volume ratio required for entry (default: 2.0)
- `DiffTradersEx` – order count ratio required for entry (default: 1.5)
- `MinDiffVolumesEx` – volume ratio used after position entry (default: 1.5)
- `MinDiffTradersEx` – order count ratio used after position entry (default: 1.3)
- `SleepMinutes` – delay between order book checks in minutes (default: 5)
- `TpPips` – take profit in price points (default: 500)
- `SlPips` – stop loss in price points (default: 500)

## Notes
The strategy does not include a Python version.
