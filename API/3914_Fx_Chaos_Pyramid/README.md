# FX Chaos Pyramid Strategy

## Overview

FX Chaos Pyramid is a multi-stage breakout strategy converted from the MetaTrader 4 "FX-CHAOS" expert advisor located in `MQL/8055`. The port keeps the original multi-timeframe logic: the primary execution happens on the 4-hour timeframe while the daily timeframe provides the higher-level breakout filters. Entries are confirmed with the Awesome Oscillator momentum filter before the first stage is opened. Additional stages pyramid into the existing position whenever the trend continues on the primary timeframe.

The StockSharp implementation uses the high-level API with candle subscriptions, indicator binding and native order helpers, so the strategy can be used both for backtesting and for live trading without extra infrastructure code.

## Trading Logic

### Higher timeframe filter

* Subscribe to daily candles and compute the last confirmed ZigZag swing using a 5-candle fractal detector.
* Store the previous day's high and low. A configurable buffer in price steps is added to both levels before breakout checks are performed.

### Primary timeframe execution

* Subscribe to 4-hour candles and bind the Awesome Oscillator (default 5/34 configuration).
* Track the latest fractal swing on the 4-hour timeframe as an analogue of the original `zzf` custom indicator.
* Record the first 4-hour candle open for each new trading day. This value plays the same role as `iOpen(NULL, 1440, 0)` in MQL.

### Entry rules

* **Initial long stage**: the current day opens below the buffered previous daily high, the 4-hour close breaks above that buffered level, the price still stays below the last daily upward fractal, and the Awesome Oscillator is negative. Existing short positions are closed before opening the long.
* **Initial short stage**: mirror logic with the daily low and the Awesome Oscillator above zero.

### Pyramid stages

After the initial stage is filled the strategy evaluates every completed 4-hour candle:

* A long addition is placed when the candle opens below and closes above the buffered previous 4-hour high while the close remains under the last upward fractal of the primary timeframe.
* A short addition uses the buffered 4-hour low and the last downward fractal.
* Optional equity filter: further stages are only permitted when the portfolio equity is greater than the balance, replicating the `AccountEquity() > AccountBalance()` requirement of the MQL expert.

The number of extra stages is configurable (up to five to match the original lot matrix). Stages reset whenever the position is closed or when a reversal signal closes the opposite side.

## Money Management

The original expert adjusts the lot matrix depending on account balance. This port keeps the same piecewise definitions and exposes the base balance, balance step and global volume multiplier as parameters. The current portfolio equity is mapped to a `MAX_Lots` bucket (ranging from 3.0 to 15.0 lots), and the appropriate lot vector is selected:

| `MAX_Lots` range | Stage 1 | Stage 2 | Stage 3 | Stage 4 | Stage 5 |
|------------------|---------|---------|---------|---------|---------|
| &lt; 2             | 0.10    | 0.50    | 0.40    | 0.30    | 0.20    |
| [2, 4)           | 0.20    | 1.00    | 0.80    | 0.60    | 0.40    |
| [4, 5)           | 0.30    | 1.50    | 1.20    | 0.90    | 0.60    |
| [5, 7)           | 0.40    | 2.00    | 1.60    | 1.20    | 0.80    |
| [7, 8)           | 0.50    | 2.50    | 2.00    | 1.50    | 1.00    |
| [8, 10)          | 0.60    | 3.00    | 2.40    | 1.80    | 1.20    |
| [10, 11)         | 0.70    | 3.50    | 2.80    | 2.10    | 1.40    |
| [11, 13)         | 0.80    | 4.00    | 3.20    | 2.40    | 1.60    |
| [13, 14)         | 0.90    | 4.50    | 3.60    | 2.70    | 1.80    |
| ≥ 14             | 1.00    | 5.00    | 4.00    | 3.00    | 2.00    |

Multiplying by the `VolumeScale` parameter allows the same relative distribution to be applied to different brokers or asset classes.

## Parameters

| Name | Description |
|------|-------------|
| **Primary Candle** | Trading timeframe used for entries (default 4 hours). |
| **Daily Candle** | Higher timeframe candles that provide previous high/low levels (default 1 day). |
| **AO Fast / AO Slow** | Short and long periods of the Awesome Oscillator. |
| **Breakout Buffer** | Buffer in price steps added to previous highs/lows. |
| **Max Stages** | Maximum number of pyramid entries (1-5). |
| **Require Profit** | Only allow additional stages when equity exceeds balance. |
| **Volume Scale** | Global multiplier applied to the selected lot vector. |
| **Base Balance** | Balance assigned to the smallest lot vector. |
| **Balance Step** | Balance increment that moves to the next vector. |

## Differences from the MQL4 Expert

* The StockSharp version uses built-in candle subscriptions instead of direct `iClose`/`iHigh` calls and stores the required price levels internally.
* The original `zzf` custom indicator is emulated through a lightweight fractal detector that confirms five-candle swings.
* Stop-loss and take-profit management is not included; the original expert modified stops dynamically, but the algorithm heavily depended on broker-specific functions. Traders can add their own risk module if required.
* Sound notifications and terminal global variables are intentionally omitted.

## Usage Tips

1. Attach the strategy to a portfolio that reports both balance and equity so that the lot matrix behaves exactly like in MetaTrader.
2. Use realistic 4-hour and daily historical data when backtesting. Mixed resolutions will degrade the pyramid logic.
3. Experiment with the `BreakoutBuffer` parameter when switching to markets that use different tick sizes or spreads.
4. Enable the chart when debugging: the strategy automatically plots candles, the Awesome Oscillator histogram and executed trades.
