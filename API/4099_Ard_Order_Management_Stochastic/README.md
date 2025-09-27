# ARD Order Management Stochastic Strategy

## Overview
The **Ard Order Management** strategy is a StockSharp conversion of the MetaTrader expert `ARD_ORDER_MANAGEMENT_EA-BETA_1`. The original script focused on repeatedly closing existing positions before placing new orders and offered helper routines for manual stop-loss and take-profit updates. The StockSharp version keeps this discipline while adding indicator-driven automation based on the Stochastic oscillator.

The default setup targets intraday forex trading on a 5-minute chart, but the candle type is fully configurable. All trading logic runs on finished candles to stay faithful to the end-of-bar execution style of the source expert.

## Trading Logic
- A Stochastic oscillator with configurable **lookback**, **signal**, and **slowing** periods generates directional signals (defaults: 5/3/3).
- When %K closes **above the buy threshold** (80 by default), the strategy cancels pending orders, closes any open short exposure, and enters a long position with the configured volume.
- When %K closes **below the sell threshold** (20 by default), all pending orders are cancelled, open long exposure is closed, and a new short is opened.
- The strategy stays in the new position until the opposite signal fires or a protective exit is triggered.

## Order and Risk Management
- Before every new entry the strategy issues market orders that fully flatten the current position, replicating the `open_order(CLOSE)` behaviour from the EA.
- `StartProtection` automatically submits initial stop-loss and take-profit orders according to the `StopLossPips` and `TakeProfitPips` parameters.
- Optional trailing logic emulates the EA's `MODIFY` branch: each finished candle can refresh a dynamic stop level (`ModifyStopLossPips`) and a floating profit target (`ModifyTakeProfitPips`). When price touches either trailing level, the position is closed to secure gains or limit risk.
- Pip conversion uses the instrument's `PriceStep` (with a 10× adjustment for fractional-pip forex symbols) so distance-based parameters stay consistent across markets.

## Parameters
- **Volume** – trading volume for new entries; additional size is added automatically to close opposing positions.
- **TakeProfitPips / StopLossPips** – initial protective distances passed to the built-in protection module. Set to zero to disable either order.
- **ModifyTakeProfitPips / ModifyStopLossPips** – trailing offsets (in pips) recalculated after every candle. Set to zero to disable trailing updates.
- **StochasticPeriod / SignalPeriod / SlowingPeriod** – oscillator configuration mirroring the `iStochastic` call from the original expert.
- **BuyThreshold / SellThreshold** – overbought/oversold levels that trigger long/short reversals.
- **CandleType** – timeframe or custom candle data source powering the indicator.

Each parameter exposes sensible optimisation ranges so you can back-test alternative settings in the StockSharp optimiser.

## Usage Notes
- Works best on liquid instruments where pip-based stops are meaningful (major forex pairs, index CFDs, liquid futures).
- Increase the timeframe when trading slower markets to reduce noise and false reversals.
- When running on live accounts, verify that the configured volume respects broker minimums and step sizes.
- The trailing logic can be disabled by leaving the `Modify*` parameters at zero, effectively reproducing the static order maintenance of the source EA.
- Combine with additional filters (trend, volatility, sessions) if you want more selective entries—the code exposes properties that can be extended.

## Conversion Details
- Source file: `MQL/9041/ARD_ORDER_MANAGEMENT_EA-BETA_1.mq4`.
- Recreated the Stochastic trigger logic hinted at in the commented `start()` routine.
- Preserved the close-before-open discipline and protective order placement through StockSharp's high-level API.
- Added optional trailing exits to reflect the EA's manual `MODIFY` block while keeping the implementation indicator-driven and event-based.
