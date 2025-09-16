# VR BUCH Moving Average Strategy

## Overview
The **VR BUCH Moving Average Strategy** is a direct port of the MetaTrader expert advisor *VR---BUCH*. It trades trend reversals using two configurable moving averages and a candle price filter. The StockSharp version keeps the original signal flow: the strategy closes open positions when an opposite setup appears and only opens a new position after the previous exposure is fully closed.

The implementation relies on StockSharp's high-level candle subscriptions, native moving average indicators and real-time order helpers. All indicator values are processed on finished candles and the strategy avoids manual historical buffers except for a small ring buffer that reproduces the MetaTrader shift parameters.

## Trading Logic
1. **Indicator calculation**
   - A fast moving average and a slow moving average are computed on the selected candle type.
   - Each moving average can use a different price source and smoothing method (simple, exponential, smoothed, weighted).
   - Optional horizontal shifts reproduce the MetaTrader `ma_shift` parameter by referencing values from past candles.
2. **Signal detection**
   - A *buy* setup occurs when the shifted fast MA is above the shifted slow MA **and** the selected confirmation price is above the fast MA.
   - A *sell* setup occurs when the shifted fast MA is below the shifted slow MA **and** the confirmation price is below the fast MA.
3. **Position handling**
   - If a position is already open, an opposite signal triggers a flat close first. New entries are evaluated on subsequent signals only when the net position returns to zero.
   - When no position exists, the strategy submits a market order with the configured volume in the direction of the active signal.

No stop-loss or take-profit levels are included by default. Users can combine the strategy with StockSharp protective blocks (`StartProtection`) or external risk managers if required.

## Parameters
| Parameter | Description |
| --- | --- |
| **Fast Period** | Length of the fast moving average. |
| **Fast Shift** | Number of candles used to shift the fast MA value into the past. |
| **Fast Price** | Candle price component used for the fast MA (close, open, high, low, median, typical, weighted). |
| **Fast Method** | Smoothing method for the fast MA (simple, exponential, smoothed, weighted). |
| **Slow Period** | Length of the slow moving average. |
| **Slow Shift** | Number of candles used to shift the slow MA value. |
| **Slow Price** | Candle price component for the slow MA. |
| **Slow Method** | Smoothing method for the slow MA. |
| **Signal Price** | Candle price used to confirm the entry (defaults to close). |
| **Candle Type** | Timeframe or custom candle type used for calculations. |
| **Volume** | Order volume for new trades. |

## Usage Notes
- Signals are evaluated only on finished candles to avoid intra-bar noise.
- The strategy expects the trading connector to provide sufficient historical data to warm up both moving averages and their shift buffers.
- Weighted price uses the formula \((High + Low + 2 * Close) / 4\), matching the MetaTrader `PRICE_WEIGHTED` option.
- The class name and namespace follow the StockSharp project conventions, enabling seamless compilation inside the `AlgoTrading` solution.

## How to Run
1. Place the strategy into a StockSharp strategy container or sample runner.
2. Configure the desired security, timeframe (`Candle Type`) and order volume.
3. Adjust moving average settings to match the original MetaTrader template if necessary.
4. Start the strategy. It will subscribe to candles, draw indicators on charts (if available) and place market orders based on the described logic.

For portfolio or multiple-symbol usage, duplicate the strategy instance per instrument and assign dedicated securities.
