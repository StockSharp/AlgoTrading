# MACD Zero Filtered Cross

## Overview
MACD Zero Filtered Cross is a C# port of the MetaTrader 4 expert advisor `Robot_MACD_12.26.9`. The original robot watches for
crossovers between the MACD line and its signal line, but filters new trades so that long entries only occur while both lines
remain below the zero axis and short entries only occur while both lines remain above the axis. The StockSharp version keeps the
same crossover logic, adds risk controls that integrate with the framework (portfolio balance filtering and unified take-profit
management), and exposes every configurable value through strategy parameters that support optimization.

The strategy relies on finished candles from a configurable timeframe. Indicator values are supplied by the built-in
`MovingAverageConvergenceDivergenceSignal` indicator, ensuring that the strategy stays compatible with the high-level API and
respects the `BindEx` usage guidelines.

## Strategy logic
### Indicator calculation
* **MACD line** – difference between a fast and slow exponential moving average (default lengths: 12 and 26).
* **Signal line** – exponential moving average applied to the MACD line (default length: 9).
* **Zero filter** – the sign of both lines relative to zero determines whether a crossover can trigger a position entry.

### Entry rules
* **Long setup**
  * The MACD line must cross above the signal line (`MACD[t-1] < Signal[t-1]` and `MACD[t] > Signal[t]`).
  * Both the MACD line and the signal line must be below zero after the crossover.
  * The current net position must be flat or short; existing shorts are closed immediately before attempting a long.
  * An optional balance filter requires the portfolio value to exceed a configurable minimum before a new order is sent.
* **Short setup**
  * The MACD line must cross below the signal line (`MACD[t-1] > Signal[t-1]` and `MACD[t] < Signal[t]`).
  * Both indicator lines must be above zero after the crossover.
  * The current net position must be flat or long; existing longs are flattened before a new short is sent.
  * The balance filter is applied symmetrically to short entries.

### Exit rules
* **Crossover exit** – when the MACD line crosses back through the signal line against the current position, the strategy closes
the open trade at market. This mirrors the original EA, which always flattened the position on an opposing crossover before
looking for new opportunities.
* **Fixed take-profit** – a unit-based take-profit (expressed in price points) is applied via `StartProtection`. The level matches
the MQL parameter `TakeProfit` and uses the instrument’s point value.

### Risk and capital management
* **Volume handling** – the `LotVolume` parameter mirrors the MT4 lot size. The strategy submits that exact volume for each entry.
* **Balance filter** – the `MinimumBalancePerVolume` parameter multiplies the requested volume to determine the minimal portfolio
value required before new entries are allowed. If the balance check fails the strategy logs a message and skips the trade,
matching the original free-margin safeguard.
* **Data integrity** – signals are processed only on finished candles and after `IsFormedAndOnlineAndAllowTrading()` confirms that
both the connection and indicators are ready.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `FastPeriod` | EMA length of the fast MACD component. |
| `SlowPeriod` | EMA length of the slow MACD component. |
| `SignalPeriod` | EMA length of the MACD signal line. |
| `TakeProfitPoints` | Distance to the protective take-profit in price points. Set to `0` to disable. |
| `LotVolume` | Base order volume, equivalent to the “Lots” input of the MT4 version. |
| `MinimumBalancePerVolume` | Minimum portfolio value required per traded volume unit before opening a position. Set to `0` to skip the filter. |
| `CandleType` | Timeframe used to build candles and feed the indicator chain. |

## Additional notes
* The strategy uses the `BindEx` overload so that the MACD indicator can supply both the MACD and signal values in a single
callback without manual calls to `GetValue`.
* All comments inside the C# code are written in English, matching the project guidelines.
* There is no Python translation for this strategy; only the C# implementation is provided in the API package.
* To replicate the original MT4 behaviour most closely, select a candle timeframe that matches the chart where the EA used to run
and keep the volume parameter consistent with the lot size previously traded.
