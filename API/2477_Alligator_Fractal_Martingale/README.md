# Alligator Fractal Martingale Strategy

This strategy ports the MetaTrader expert "Alligator(barabashkakvn's edition)" to StockSharp's high-level API. It combines Bill Williams' Alligator indicator with fractal breakout confirmation, an averaging martingale ladder and adaptive trailing stops. The logic is designed for hedging-style execution where the first order is opened at market and additional entries are scheduled at predefined distances when price moves against the position.

## Trading logic

- **Alligator mouth expansion** – the lips (green), teeth (red) and jaw (blue) smoothed moving averages are processed on the median price. A long bias is activated when the lips rise above the jaw by at least `EntrySpread`, while a short bias requires the opposite alignment. When the spread contracts below `ExitSpread`, the respective bias is disabled.
- **Fractal filter (optional)** – finished candles are scanned for Bill Williams fractals. A long signal is accepted only if an up fractal within the last `FractalLookback` bars remains at least `FractalBuffer` above the close. Short signals require a down fractal below the market. Disable the filter through `UseFractalFilter` to enter on Alligator signals alone.
- **Martingale averaging** – after the initial market order the strategy can pre-build `MartingaleSteps` averaging levels spaced by `MartingaleStepDistance`. Each level multiplies the previous volume by `MartingaleMultiplier` (capped by `MaxVolume`) and is executed once price touches the level.
- **Trailing exit management** – every filled long or short position receives a synthetic stop-loss and take-profit based on `StopLossDistance` and `TakeProfitDistance`. When `EnableTrailing` is on, stops are pulled forward by at least `TrailingStep` as the market moves in favour of the trade.
- **Alligator exits (optional)** – when `UseAlligatorExit` is true, the position is closed as soon as the Alligator mouth closes (bias flips from active to inactive).

## Risk and order handling

- The strategy uses the `Volume` parameter for the first market order. Each martingale level reuses the rounded volume and multiplies it by the configured factor while keeping the result below `MaxVolume`.
- Stops and targets are evaluated internally on every finished candle instead of relying on native exchange orders. When the candle range crosses the synthetic stop or target the position is flattened immediately.
- Opposite positions are flattened before a new direction is opened to avoid hedged exposure inside StockSharp.

## Parameters

| Parameter | Description |
| --- | --- |
| `Volume` | Base order size for the first market entry. |
| `JawLength`, `TeethLength`, `LipsLength` | Length of the smoothed moving averages forming the Alligator jaw, teeth and lips. |
| `JawShift`, `TeethShift`, `LipsShift` | Forward shift (in bars) applied when reading the Alligator buffers. |
| `EntrySpread`, `ExitSpread` | Minimum spread to enable trades and contraction threshold to disable them. |
| `UseAlligatorEntry`, `UseAlligatorExit` | Toggle Alligator-based entries and exits. |
| `UseFractalFilter` | Enable or disable the fractal confirmation layer. |
| `FractalLookback`, `FractalBuffer` | Lookback window and safety margin for valid fractals. |
| `EnableMartingale`, `MartingaleSteps`, `MartingaleMultiplier`, `MartingaleStepDistance`, `MaxVolume` | Control the averaging ladder. |
| `StopLossDistance`, `TakeProfitDistance`, `EnableTrailing`, `TrailingStep` | Configure synthetic risk management. |
| `AllowMultipleEntries` | Allow repeated market entries while a position is open. |
| `ManualMode` | When true the algorithm only manages open trades and does not create new ones. |
| `CandleType` | Source candle series for indicator calculations. |

## Usage notes

1. Ensure the selected instrument supports the configured price and volume steps; the strategy rounds the values using `Security.MinPriceStep` and `Security.VolumeStep` when available.
2. The martingale ladder is simulated internally. If you prefer using actual limit orders on the exchange, disable the feature and manage scaling externally.
3. Start the strategy in a hedging-compatible portfolio. Even though StockSharp aggregates the net position, the original logic assumes the ability to add multiple legs in the same direction.
4. Review the default pip-based distances (`0.008` ≈ 80 pips for four-digit FX quotes) and adjust them to the instrument being traded.
