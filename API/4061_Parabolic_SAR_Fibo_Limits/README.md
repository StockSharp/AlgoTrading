# Parabolic SAR Fibo Limits

## Overview
Parabolic SAR Fibo Limits is a StockSharp port of the MetaTrader 4 expert advisor `FT_0tk80i9uw4ep_Parabolic`. The original robot combines a dual Parabolic SAR stack with Fibonacci retracement levels to stage limit entries at key pullback zones. The C# strategy preserves the staged order placement, the built-in break-even and trailing protections, and the optional trading session filter so that the behaviour matches the source EA when it is attached to a chart with finished candles.

## Strategy logic
### Signal preparation
* **Dual Parabolic SAR alignment** – two Parabolic SAR indicators are calculated on the same timeframe. The fast SAR is used as an early warning, while the slow SAR confirms the state change. When the fast SAR jumps above the price while the slow SAR remains below it, the strategy arms a potential long setup. When the fast SAR dips below the price while the slow SAR stays above it, a potential short setup is armed. The setups are cleared as soon as the slow SAR crosses the price in the respective direction.
* **Swing detection** – the strategy queries the highest high and lowest low over the configurable `Bar Search` window to replicate the `MaximumMinimum` helper from the EA. The previous finished candle provides the opposing extreme (`High[1]` or `Low[1]`) that anchors the Fibonacci calculations.

### Order placement and management
* **Fibonacci pending orders** – once both SARs sit on the same side of the price and a setup is armed, the strategy submits a limit order at the 50% Fibonacci level (`Entry Fibonacci %`) of the detected swing. The protective stop is offset from the swing extreme by the configured number of points, and the take profit is placed at the extended Fibonacci projection (`Target Fibonacci %`). Orders are only accepted when the current price, the planned stop, and the target are all at least five price steps away from each other, mirroring the EA’s `Point*5` safety filter.
* **Automatic order cleanup** – whenever the fast SAR crosses back over the price, the pending limit order for that direction is cancelled to avoid entering in the wrong market phase. Filling a limit order automatically cancels the opposite pending order.

### Risk management
* **Initial stop and target** – the EA’s pending order stop-loss and take-profit parameters are emulated by applying the calculated stop and target levels as soon as the limit order is filled.
* **Break-even shift** – if `Break Even (points)` is greater than zero, the stop moves to the entry price plus one price step (or minus one step for shorts) once the trade gains the specified number of points, reproducing the original BBU routine.
* **Trailing stop** – when `Trailing Stop (points)` is enabled, the stop follows the price by the chosen distance. The stop is only updated when the new stop improves the previous one by at least `Trailing Step (points)`, matching the EA’s `TrailingShag` behaviour.
* **Manual exit triggers** – if price touches the calculated stop or target levels on a finished candle, the position is closed with a market order to simulate MT4’s automatic order execution.

### Time filter
* **Optional session control** – enabling `Use Time Filter` restricts new entries to the inclusive window between `Start Hour` and `Stop Hour` in exchange time. Protective logic (break-even, trailing, exits) continues to operate even outside the session, just like in the MQL implementation.

## Parameters
* **Use Time Filter** – toggles the trading session filter.
* **Start Hour / Stop Hour** – inclusive session hours used when the time filter is enabled.
* **Fast SAR Step / Fast SAR Max** – acceleration factor and maximum acceleration for the fast Parabolic SAR.
* **Slow SAR Step / Slow SAR Max** – acceleration factor and maximum acceleration for the slow Parabolic SAR.
* **Bar Search** – number of bars included in the swing high/low calculation.
* **Offset (points)** – number of price steps added beyond the swing extreme when computing the stop-loss.
* **Entry Fibonacci %** – Fibonacci percentage (expressed as 0–200+) used for the limit order price.
* **Target Fibonacci %** – Fibonacci percentage applied to compute the take-profit projection.
* **Break Even (points)** – profit in points required before the stop jumps to the entry price (+/- one step). Set to `0` to disable.
* **Trailing Stop (points)** – distance between price and trailing stop. Set to `0` to disable trailing.
* **Trailing Step (points)** – minimum improvement (in points) before the trailing stop is advanced.
* **Candle Type** – timeframe that drives the indicator and swing calculations.
* **Volume** – base order volume inherited from the StockSharp `Strategy` class (default `0.1`).

## Additional notes
* All point-based parameters are automatically converted into price offsets using the instrument’s price step. Five-digit FX symbols, indices, and other assets therefore reuse the EA settings without manual scaling.
* The strategy processes only finished candles supplied by the configured subscription, exactly matching the EA’s bar-by-bar execution.
* There is no Python version of this strategy; only the C# implementation is available in the API package.
