# IFS Fractals

## Overview
IFS Fractals is a port of the MetaTrader 5 script `IFS_Fractals`. The original expert renders an iterated function system (IFS) bitmap of the "fractal word" by repeatedly applying 28 affine transforms to a point cloud. The StockSharp version turns the same chaotic process into a directional oscillator: the X coordinate of the generated points is scaled, smoothed with an exponential moving average (EMA), and interpreted as a momentum gauge that drives long and short entries.

## Strategy logic
### Iterated function system
* **Affine transforms** – every finished candle triggers a batch of iterations (configurable). During each iteration one of the 28 transforms is selected according to the original probability weights (all equal to 35). The transform updates the current point `(x, y)` using the coefficients ported verbatim from the MQL5 code.
* **Probability table** – the strategy precalculates a cumulative probability array once on start, allowing fast selection of the next transform using a single random draw within the total probability mass.

### Signal construction
* **Normalization** – the X coordinate is divided by the same scale factor (`50` by default) that the script used when projecting the fractal onto the bitmap. This keeps the signal in a stable numeric range regardless of the instrument price.
* **EMA smoothing** – the normalized series feeds an EMA whose period is configurable. The EMA acts as a low-pass filter that extracts the dominant drift of the chaotic iterations.
* **Entry logic** – when the EMA rises above the positive entry threshold the strategy opens or reverses into a long position. Symmetrically, when the EMA falls below the negative threshold it opens or reverses into a short.
* **Exit logic** – open longs exit once the EMA drops back to or below the exit threshold, while shorts exit when the EMA climbs back above the negative exit threshold. This creates a hysteresis band that avoids rapid flip-flopping around zero.

### Risk management
* **Position protection** – optional absolute stop-loss and take-profit distances can be enabled through `StartProtection`. A value of `0` disables the respective level, matching the behaviour of the source script that operated without protective orders.
* **Volume control** – entries use a fixed market volume parameter. Any existing opposite exposure is closed before a new trade is opened to maintain a single directional position.

## Parameters
* **Volume** – market volume for new entries.
* **Candle Type** – timeframe that drives the fractal iterations (default: 5-minute candles).
* **Iterations** – number of IFS iterations processed after every finished candle.
* **Scale** – divisor applied to the X coordinate before feeding it into the EMA.
* **Entry Threshold** – absolute EMA value required to open a position (positive for longs, negative mirrored for shorts).
* **Exit Threshold** – EMA value that triggers exits when the signal reverts toward zero.
* **EMA Period** – smoothing period of the exponential moving average applied to the normalized fractal signal.
* **Take Profit** – absolute take-profit distance; set to `0` to disable.
* **Stop Loss** – absolute stop-loss distance; set to `0` to disable.

## Additional notes
* Each run produces a different trade sequence unless a deterministic random seed is injected by modifying the source; this mirrors the randomness of the original bitmap rendering script.
* The strategy does not require any market-derived indicators. All data is generated internally from the IFS coefficients, so the subscribed candles simply provide timing for the iterations.
* No Python implementation is included in this package. Only the C# strategy is available under `CS/`.
