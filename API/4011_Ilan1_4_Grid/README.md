# Ilan 1.4 Basket Grid Strategy

## Overview
Ilan 1.4 is a classical averaging grid system. The converted strategy subscribes to a single candle series and opens an initial market position based on the direction of the last two completed candles: if the more recent close is below the older one the basket starts with a sell, otherwise it opens a buy. When price moves against the active basket by the configured **Pip Step**, the strategy optionally adds a new position in the same direction and recalculates the weighted-average entry price.

All trades inside the basket are executed with market orders. When the closing price reaches the average entry price plus the **Take Profit** distance, the whole basket is closed. A trailing stop, a fixed stop loss, an equity-based emergency stop and a maximum life time safeguard reproduce the protection blocks from the original MetaTrader expert.

## Trading Rules
1. Wait for the next finished candle and evaluate the last two closes.
2. If there is no exposure, open a long basket when the latest close is higher than the previous one and a short basket otherwise.
3. Keep track of the latest fill price and the weighted-average entry price of the active basket.
4. When **Use Add** is enabled and the price moves against the position by **Pip Step** points, calculate the next lot size and open an additional market trade. If **Close Before Adding** is enabled the existing basket is closed first and re-opened with the scaled volume.
5. Recalculate the average entry price after every fill. The basket is liquidated once price touches the averaged take-profit level or when any of the risk rules fires.
6. Once a basket is closed the logic immediately prepares a new signal using the last two candle closes.

## Money Management Modes
The **Money Management** parameter reproduces the original `MMType` switch:
- **Fixed** – every new order uses the configured **Initial Volume**.
- **Geometric** – subsequent orders multiply the base volume by `LotExponent^n`, where `n` equals the current number of open trades.
- **RecoverLastLoss** – after a losing basket the next position uses the volume of the last closed trade multiplied by **Lot Exponent**; profitable baskets reset the volume back to the base value.

Trade volumes are rounded according to **Volume Digits** and the security volume step. When rounding would produce zero the unrounded input volume is used instead.

## Risk Controls
- **Take Profit** – closes the whole basket once price reaches the average entry price ± configured points.
- **Stop Loss** – closes the basket when price moves against the average entry price by the specified number of points.
- **Use Trailing Stop** with **Trail Start** and **Trail Stop** – activates a trailing level once the basket earns enough points; the trailing offset follows price to protect profits.
- **Use Equity Stop** with **Equity Risk %** – monitors the portfolio value and closes the basket when the floating loss exceeds the chosen percentage of the recorded equity peak.
- **Use Timeout** with **Max Open Hours** – forcefully closes the basket when it remains open longer than the allowed number of hours.

## Parameters
- **Candle Type** – timeframe used for generating trading signals.
- **Initial Volume** – starting lot size for a new basket.
- **Volume Digits** – precision used when rounding calculated volumes.
- **Money Management** – volume calculation mode (`Fixed`, `Geometric`, `RecoverLastLoss`).
- **Lot Exponent** – multiplier applied by the geometric and recovery schemes.
- **Close Before Adding** – close all open trades before placing the next averaging order.
- **Use Add** – enable or disable averaging orders altogether.
- **Pip Step** – minimum adverse movement (in price steps) before adding a new trade.
- **Take Profit** – profit target from the average entry price.
- **Stop Loss** – maximum allowed adverse excursion from the average entry price.
- **Use Trailing Stop / Trail Start / Trail Stop** – trailing-stop configuration.
- **Max Trades** – maximum number of averaging trades allowed inside a basket.
- **Use Equity Stop / Equity Risk %** – parameters of the floating-loss protection.
- **Use Timeout / Max Open Hours** – lifespan control for each basket.

## Conversion Notes
- MetaTrader pending order helpers were replaced with direct market orders because the averaging logic always executed immediately in the original code.
- The trailing block now works on the aggregated basket instead of modifying each order separately; the trigger distances match the original defaults.
- Portfolio equity is monitored through the StockSharp portfolio object to emulate the expert’s equity-stop routine.
- Position averages and basket statistics are calculated inside the strategy without storing per-trade collections, respecting the high-level API guidelines.
