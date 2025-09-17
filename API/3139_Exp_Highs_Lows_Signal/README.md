# Exp Highs Lows Signal

## Overview
Exp Highs Lows Signal is a direct port of the MetaTrader 5 expert advisor `Exp_HighsLowsSignal`. The strategy relies on a pattern detector that searches for a configurable number of consecutive candles that print higher highs and higher lows (bullish sequence) or lower highs and lower lows (bearish sequence). Once a sequence is found, the strategy delays the reaction by the configured number of closed bars, closes any opposite exposure, and optionally opens a position in the detected direction. Protective stops are expressed in price steps to mirror the point-based money management used by the original robot.

## Strategy logic
### Highs/Lows sequence detector
* The detector evaluates each finished candle on the selected timeframe.
* A **bullish signal** requires `SequenceLength` consecutive comparisons where both the current high and the current low are strictly greater than the previous bar.
* A **bearish signal** requires `SequenceLength` consecutive comparisons where both the current high and the current low are strictly lower than the previous bar.
* Signals are queued and released after `SignalBarDelay` closed candles, matching the `SignalBar` setting from the MQL implementation.

### Entry rules
* **Long entries**
  * Triggered when a bullish sequence becomes active and `AllowLongEntry` is enabled.
  * Any existing short position is closed first (if `AllowShortExit` is true), then a market buy order is submitted with volume `OrderVolume + |Position|` to cover shorts and establish the desired long size.
* **Short entries**
  * Triggered when a bearish sequence becomes active and `AllowShortEntry` is enabled.
  * Any existing long position is closed first (if `AllowLongExit` is true), then a market sell order is submitted with volume `OrderVolume + |Position|` to cover longs and establish the desired short size.

### Exit rules
* A bullish sequence always requests `AllowShortExit` to close open shorts.
* A bearish sequence always requests `AllowLongExit` to close open longs.
* When the relevant flag is disabled, the opposite exposure remains untouched, allowing the user to trade only one direction or run the detector in "alerts only" mode.

### Risk management
* `StopLossTicks` and `TakeProfitTicks` represent distances in price steps (points). A value of `0` disables the corresponding protective order, reproducing the behaviour of the original EA.
* `StartProtection` converts those distances into absolute price offsets so that all market entries automatically receive matching stop-loss and take-profit orders.

## Parameters
* **OrderVolume** – base order volume used when a new trade is opened.
* **AllowLongEntry / AllowShortEntry** – toggles that enable long or short entries on their respective signals.
* **AllowLongExit / AllowShortExit** – toggles that allow the strategy to flatten opposite positions when the counter-trend signal appears.
* **StopLossTicks / TakeProfitTicks** – protective distances in price steps; set to `0` to disable.
* **SequenceLength** – number of consecutive comparisons required to qualify a bullish or bearish sequence (equivalent to `HowManyCandles` in MT5).
* **SignalBarDelay** – number of closed candles to wait before acting on a signal (equivalent to the `SignalBar` input).
* **CandleType** – timeframe used to build the Highs/Lows detector (default: 4-hour candles).

## Additional notes
* The strategy stores only the minimal amount of candle history required for the detector, keeping the behaviour identical to the MT5 custom indicator.
* Because all order management occurs through `StartProtection`, backtests and live trading automatically receive matching stop and take-profit orders without extra code.
* Disable the corresponding `Allow` flags to turn the strategy into a directional filter or a pure signalling tool.
* No Python translation is provided; only the C# version is available in this package.
