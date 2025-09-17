# Exp Cronex MFI Strategy

## Overview
The strategy replicates the **Exp_CronexMFI** expert advisor. It smooths the Money Flow Index (MFI) twice and trades **against** the crossover of the resulting lines. The port keeps the original contrarian philosophy while exposing every setting as a StockSharp strategy parameter.

## How it works
1. Subscribe to the selected candle series (the default is 4-hour time frame).
2. Calculate the Money Flow Index with the configured period.
3. Apply the chosen smoothing method twice: the first pass produces the fast Cronex line, the second pass smooths the fast line again to build the slow line.
4. Store historical pairs of fast and slow values with an adjustable delay (`SignalShift`).
5. When the fast line crosses **down** through the slow line, close shorts (if allowed) and open/extend a long position. When the fast line crosses **up**, close longs and open/extend a short position.
6. Orders are sent with the strategy `Volume` and can be disabled independently for long and short sides.

The strategy only evaluates finished candles to match the timing of the MetaTrader implementation.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `MfiPeriod` | `int` | `25` | Length of the Money Flow Index. |
| `FastPeriod` | `int` | `14` | Period of the first smoothing stage (fast Cronex line). |
| `SlowPeriod` | `int` | `25` | Period of the second smoothing stage (slow Cronex line). |
| `SignalShift` | `int` | `1` | Number of completed candles to delay signal processing, reproducing the `SignalBar` behaviour from MQL. |
| `Smoothing` | `SmoothingMethod` | `Simple` | Moving-average algorithm used for both smoothing stages. |
| `EnableLongEntries` | `bool` | `true` | Enables market orders that open or add to long positions. |
| `EnableShortEntries` | `bool` | `true` | Enables market orders that open or add to short positions. |
| `EnableLongExits` | `bool` | `true` | Allows signals to close existing long exposure. |
| `EnableShortExits` | `bool` | `true` | Allows signals to close existing short exposure. |
| `CandleType` | `DataType` | `TimeFrame(4h)` | Candle series used for indicator calculations. |
| `Volume` | `decimal` | `1` | Order size used when opening new positions. |

## Smoothing options
The original MQL indicator offers several proprietary smoothing modes. The StockSharp port maps them to built-in moving averages:

| MQL concept | `SmoothingMethod` value | Notes |
| --- | --- | --- |
| SMA | `Simple` | Simple moving average. |
| EMA | `Exponential` | Exponential moving average. |
| SMMA | `Smoothed` | Smoothed moving average (Wilder). |
| LWMA | `Weighted` | Linear weighted moving average. |
| JJMA / JurX / ParMA / T3 / VIDYA / AMA | `DoubleExponential`, `TripleExponential`, `Hull`, `ZeroLagExponential`, `ArnaudLegoux`, `KaufmanAdaptive` | Select the closest approximation for adaptive smoothing. |

## Differences vs MQL version
- Tick/real volume selection from MQL is not available; StockSharp candles provide aggregate volume data.
- Trade management relies on market orders only. The original money-management helper that delayed execution until the next bar is emulated through `SignalShift`.
- Stop-loss and take-profit placement must be configured externally (for example, via risk rules or protection modules).

## Usage notes
- Choose a candle series that matches the instrument liquidity; the default 4-hour interval mirrors the source EA.
- Fine-tune `SignalShift` if you want to confirm a crossover with additional candles.
- Combine the strategy with risk-management rules (e.g., `StartProtection`) to cap losses.
