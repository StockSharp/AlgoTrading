# VR Moving Distance Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This StockSharp strategy replicates the VR-Moving expert advisor from MetaTrader 5. It watches a configurable moving average and reacts when price drifts beyond a fixed pip distance. The algorithm can scale into trends by multiplying the base order volume for follow-up trades and applies simple take-profit logic while only one position is open.

## Overview
- Trades the instrument assigned to the strategy using a single candle subscription.
- Computes a moving average with selectable length, smoothing type, and price source.
- Converts distance and take-profit settings from pips into price offsets using the security price step.
- Adds long positions when price rises far enough above the moving average, or short positions when price falls below it.
- Reverses the current net exposure before opening a position in the opposite direction to keep the portfolio netting-friendly.

## Indicators and Data
- One moving average (`Simple`, `Exponential`, `Smoothed`, `Weighted`, or `VolumeWeighted`).
- Candles arrive with the configured `Candle Type`; the same stream drives indicator values and trading decisions.

## Entry Logic
1. On each finished candle the strategy waits for the moving average to be fully formed.
2. If the high of the bar is at least `DistancePips` above the moving average, a long entry is triggered.
3. If the low of the bar is at least `DistancePips` below the moving average, a short entry is triggered.
4. When switching direction the strategy closes the existing exposure by adding the opposite volume to the new market order.

## Scaling and Volume Management
- The first order uses the configured `BaseVolume`.
- Subsequent orders in the same direction use `BaseVolume * VolumeMultiplier`.
- The highest filled price on the long side and the lowest filled price on the short side are tracked. Each new scaling order requires price to extend by another `DistancePips` from that extreme before firing.

## Exit Logic
- When exactly one long position is open, a profit target is placed at the entry price plus `TakeProfitPips` (converted to price units). If a candle high touches the target, the position is closed.
- Similarly, a single short position receives a profit target at entry minus `TakeProfitPips` and closes when the candle low touches it.
- Once multiple entries exist the strategy keeps the positions open and waits for new scaling signals; no averaged exit is attempted in this port.

## Risk Management Notes
- `StartProtection()` is activated on start to plug into the standard StockSharp protective subsystems.
- Distance and take-profit values are measured in pips. For symbols quoted with 3 or 5 decimal places the strategy multiplies the price step by 10 to match MetaTrader pip semantics.
- There is no automatic stop-loss; risk must be controlled through the chosen parameters and external portfolio limits.

## Parameters
- **Candle Type** – Data type used for candle subscription.
- **MA Length** – Period of the moving average.
- **MA Type** – Moving average smoothing method.
- **Price Source** – Candle price used to calculate the moving average.
- **Distance (pips)** – Minimum pip gap between price and the moving average to trigger entries.
- **Take Profit (pips)** – Profit target distance applied when only one position is open.
- **Volume Multiplier** – Multiplier applied to the base volume for additional entries.
- **Base Volume** – Quantity of the initial trade.
