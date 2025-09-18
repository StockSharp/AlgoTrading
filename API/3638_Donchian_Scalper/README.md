# Donchian Scalper

## Overview
Donchian Scalper is a StockSharp port of the MetaTrader 4 expert advisor `DonchianScalperEA`. The strategy monitors Donchian channel boundaries and the exponential moving average (EMA) of the same length. A pending stop order is armed only after price pulls back through the EMA, signalling that momentum has reset before a potential breakout. Entries are executed with stop orders placed at the current Donchian extremes and protected by the opposite band. Profits are managed either by a fixed take-profit distance or by adaptive trailing stops that track the chosen market structure.

## Strategy logic
### Entry preparation
* **Pullback validation** – the strategy waits until one of the two previously closed candles crosses below the EMA (for longs) or above the EMA (for shorts). The crossing level is offset by the configurable *Cross Anchor* distance to ensure the pullback is meaningful.
* **Breakout arming** – once the pullback condition is satisfied and the cooldown timer has expired, a stop order is submitted at the most recent Donchian boundary (upper band for longs, lower band for shorts). The opposite band defines the initial protective stop. Existing pending orders are automatically realigned when the Donchian levels flatten for at least two candles.

### Trade management
* **Initial protection** – when a breakout order fills, the strategy places a protective stop order using the precomputed stop price. The stop level equals the opposite Donchian band and can be shifted inward by the *Stop Loss (points)* setting.
* **Profit control** – two management modes are available:
  * *Close At Profit* – closes the position once the net movement from the average entry price exceeds the configured take-profit distance.
  * *Trailing* – keeps the trade open and periodically tightens the protective stop. The trailing engine can follow the Donchian boundary, the EMA, or an ATR-based volatility band.
* **Cooldown** – after all positions are closed, the strategy waits for the specified number of finished candles before arming new breakout orders. This reproduces the MetaTrader logic that requires at least three bars between trades.

## Parameters
* **Volume** – order volume used for stop entries and market exits.
* **Channel Period** – Donchian channel length, also used for the EMA filter.
* **Cross Anchor** – additional distance (in points) that the pullback must exceed before the breakout order is armed.
* **Stop Loss (points)** – distance added to the opposite Donchian band for the initial protective stop; set to `0` to place the stop directly on the band.
* **Take Profit (points)** – profit target used by the *Close At Profit* mode. Ignored when the trailing mode is active.
* **Candle Type** – timeframe driving indicator calculations.
* **Profit Mode** – selects between the fixed take-profit exit and adaptive trailing stops.
* **Trailing Mode** – trailing engine used in the *Trailing* profit mode. Choices are Donchian boundary, EMA, or ATR-based trailing.
* **Cooldown Bars** – minimum number of finished candles that must pass after the position becomes flat before new orders may be placed.
* **ATR Period / ATR Multiplier** – parameters for the ATR trailing engine. The multiplier defines how many ATRs are subtracted (long) or added (short) to compute the trailing stop.

## Additional notes
* The strategy aligns every stop and entry price to the instrument’s price step to ensure exchange compliance.
* When both long and short stop orders are active, filling one side will automatically cancel the opposite pending order to avoid hedging.
* If *Take Profit (points)* is set to zero while the profit mode remains *Close At Profit*, the strategy will keep positions open until the protective stop is hit.
* The conversion focuses on the high-level StockSharp API: indicator binding, candle subscriptions, and helper methods (`BuyStop`, `SellStop`, `SellMarket`, etc.). Python implementation is not included in this package.
