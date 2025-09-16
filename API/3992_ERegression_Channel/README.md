# E Regression Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The **E Regression Channel Strategy** reproduces the MetaTrader "e-Regr" expert advisor using StockSharp's high-level strategy API. It fits a polynomial regression curve to recent closing prices, builds equidistant bands from the residual standard deviation and reacts when price pierces those envelopes. The strategy is designed for mean-reversion trading with optional protective stops, a daily volatility filter and an intraday trading window.

## Trading Logic
1. Subscribe to the main timeframe specified by `Candle Type` and compute a polynomial regression channel on the last `Regression Length` closes.
2. The middle band is the regression fit; the upper and lower bands are shifted by `Std Dev Multiplier` multiplied by the residual standard deviation.
3. Close any existing long position when the candle close crosses above the middle band; close short positions when the close drops below it.
4. Open a long position (after closing any existing short exposure) when the current candle low touches or breaks the lower band.
5. Open a short position (after flattening long exposure) when the current candle high touches or breaks the upper band.
6. Optionally trail open positions using `Trailing Activation` and `Trailing Distance` once price moves far enough in favour of the trade.
7. Skip new entries whenever the previous daily candle's range exceeds the `Daily Range Filter` threshold or the current time is outside the `[Trade Start, Trade End)` window.

## Parameters
- `Volume` – order size used for every market entry (net positions are flattened before reversing).
- `Trade Start` / `Trade End` – daily trading window, supports overnight ranges (e.g. 21:00–02:00).
- `Regression Length` – number of candles used for the polynomial regression fit.
- `Degree` – polynomial degree (1–6) applied to the regression model.
- `Std Dev Multiplier` – multiplier applied to the regression residual standard deviation to form the bands.
- `Enable Trailing` – toggles trailing stop management.
- `Trailing Activation` – number of points of favourable movement required before trailing starts.
- `Trailing Distance` – trailing buffer maintained once trailing is active (in points).
- `Stop Loss` – protective stop distance in points (0 disables automatic stop).
- `Take Profit` – protective profit target distance in points (0 disables automatic target).
- `Daily Range Filter` – maximum allowed range of the previous daily candle, expressed in points.
- `Candle Type` – timeframe for the primary price series (default 30-minute time frame).

## Default Settings
- `Volume` = 0.1
- `Trade Start` = 03:00
- `Trade End` = 21:20
- `Regression Length` = 250 bars
- `Degree` = 3
- `Std Dev Multiplier` = 1.0
- `Enable Trailing` = false
- `Trailing Activation` = 30 points
- `Trailing Distance` = 30 points
- `Stop Loss` = 0 points (disabled)
- `Take Profit` = 0 points (disabled)
- `Daily Range Filter` = 150 points
- `Candle Type` = 30-minute candles

## Additional Notes
- The strategy uses the latest finished candle for all decisions and never trades multiple times within the same bar.
- Trailing stops close positions by market when price touches the internally calculated trailing level.
- If the previous day is too volatile (range above the configured filter), existing positions are closed and new entries are suspended for the remainder of the bar.
- The regression channel is redrawn on the chart at every update to help visualise the middle, upper and lower bands.
