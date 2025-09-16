# Price Extreme Strategy

## Overview

The **Price Extreme Strategy** replicates the MetaTrader expert adviser `Price_Extreme_Strategy` using the StockSharp high-level API. The system monitors a sliding channel derived from the highest high and lowest low over a configurable number of completed candles. Breakout signals are generated whenever the selected reference candle closes above the upper boundary or below the lower boundary. The logic can optionally be inverted to transform breakout conditions into fade entries.

This conversion keeps the trading workflow event-driven. Orders are submitted immediately after the close of each finished candle, matching the behaviour of the original MQL algorithm that reacted on the opening tick of the next bar.

## Indicator Logic

The Price Extreme channel is rebuilt on every finished candle using StockSharp's `Highest` and `Lowest` indicators:

- `Highest` tracks the maximum high over the last *N* candles.
- `Lowest` tracks the minimum low over the last *N* candles.

These buffers emulate the `Price_Extreme_Indicator` custom study bundled with the original expert adviser. The indicator length is exposed through the **Level Length** parameter.

A separate **Signal Shift** parameter defines which closed candle is used to evaluate the breakout condition. A shift of `1` means "use the candle that just closed" (default). Larger values allow waiting for additional confirmation by referencing older bars.

## Trading Rules

1. Recalculate upper and lower channel values for every finished candle.
2. Retrieve the candle specified by **Signal Shift** from the internal history buffer.
3. Generate directional intents:
   - **Breakout up**: the candle's close is above the upper channel value.
   - **Breakout down**: the candle's close is below the lower channel value.
4. Apply optional inversion with **Reverse Signals**:
   - If disabled, trade in the breakout direction (buy on breakout up, sell on breakout down).
   - If enabled, swap the reactions (sell on breakout up, buy on breakout down).
5. Respect **Enable Long** and **Enable Short** permissions before submitting orders.
6. Automatically close any opposite position before opening a new trade so that only one net position exists at any time.

## Risk Management

The strategy provides stop-loss and take-profit handling that mirrors the point-based controls of the MQL version:

- **Stop Loss** and **Take Profit** are expressed in price steps (`Security.PriceStep`).
- Target prices are recalculated whenever the net position size changes.
- If a finished candle overlaps the protective levels (low below the stop for longs, high above the stop for shorts, etc.), the position is closed via market order and the protective targets are cleared.
- `StartProtection()` is enabled during `OnStarted` to leverage built-in StockSharp safeguards.

## Parameters

| Parameter | Description | Default | Group |
|-----------|-------------|---------|-------|
| `LevelLength` | Number of completed candles considered when computing the extreme channel. | 5 | Indicator |
| `SignalShift` | Index of the closed candle used for breakout validation (1 = last closed candle). | 1 | Indicator |
| `EnableLong` | Allows buying when `true`. | `true` | Trading |
| `EnableShort` | Allows selling when `true`. | `true` | Trading |
| `ReverseSignals` | Inverts breakout reactions (buy on breakdown, sell on breakout). | `false` | Trading |
| `OrderVolume` | Volume sent with each market order. Must be greater than zero. | 1 | Trading |
| `StopLossPoints` | Stop-loss distance measured in price steps. A value of `0` disables the stop. | 0 | Risk |
| `TakeProfitPoints` | Take-profit distance measured in price steps. A value of `0` disables the target. | 0 | Risk |
| `CandleType` | Primary timeframe for data subscription. | 5-minute candles | Data |

All parameters use `StrategyParam<T>` with UI metadata so they can be optimized or modified from the Designer.

## Usage Guidelines

1. Attach the strategy to a security and set the **Candle Type** to match the timeframe used in the original MetaTrader setup.
2. Adjust **Level Length** if a wider or narrower Price Extreme channel is desired.
3. Configure **Signal Shift** to control how many closed candles to wait before evaluating the breakout.
4. Select desired trade directions via **Enable Long**, **Enable Short**, and **Reverse Signals`**.
5. Define **Order Volume**, **Stop Loss**, and **Take Profit** to match risk preferences. Remember that both protective values operate in price steps.
6. Launch the strategy. Candles, indicator bands, and executed trades are plotted automatically when a chart area is available.

## Additional Notes

- The strategy intentionally operates on a single net position, mirroring the hedging logic of the MQL expert by flattening the opposite side before entering a new trade.
- Protective stops and targets are evaluated on completed candles. When live trading, this approximates the server-side protective orders used by the original script.
- No Python port is included, as requested.
