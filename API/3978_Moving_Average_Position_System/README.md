# Moving Average Position System Strategy

## Overview

The Moving Average Position System is a direct port of the MetaTrader 4 expert advisor "MovingAveragePositionSystem.mq4". The strategy monitors a long lookback moving average and reacts to price crossings that occur on completed candles. It supports both manual lot selection and an optional martingale-like volume escalation routine that reacts to accumulated profits and losses expressed in MetaTrader points.

## Trading Logic

1. **Signal detection**
   - The system calculates a configurable moving average (simple, exponential, smoothed, or linear weighted).
   - When the close of the most recently finished candle crosses the moving average in the opposite direction of the previous close, the strategy opens a new position.
   - Only one position per direction is allowed; if the strategy is already long it will not add to the position until the current one is closed, and the same applies for short trades.
2. **Position management**
   - If the candle that just closed ends back below the moving average while a long position is open, the position is immediately closed at market.
   - If the candle closes back above the moving average while a short position is open, the short is closed.
   - A MetaTrader-style take-profit expressed in price steps (points) can be activated through the strategy parameters. Stops are otherwise managed by the moving average cross.
3. **Money management**
   - When the martingale block is enabled, the strategy accumulates realized and floating PnL in MetaTrader points for the current cycle.
   - If cumulative losses exceed the configured loss threshold, the next trade volume is doubled (while never exceeding the maximum lot size) and all open positions are flattened.
   - When cumulative profits exceed the configured profit target, the volume is reset back to the starting lot size and any open positions are closed to lock in gains.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **MaType** | Moving average calculation method: Simple, Exponential, Smoothed, or LinearWeighted. Mirrors the `TypeMA` input of the original expert. |
| **MaPeriod** | Lookback period for the moving average (default 240). |
| **MaShift** | Forward shift applied to the moving average values before generating signals. Equivalent to the `SdvigMA` input. |
| **CandleType** | Candle data type used for signal calculations. Defaults to 1-hour time frame candles. |
| **InitialVolume** | Volume used before the martingale routine modifies it. Corresponds to the `Lots` input. |
| **StartVolume** | Base lot size that the martingale resets to after a profitable cycle (`StarLots`). |
| **MaxVolume** | Upper limit for the trade volume (`MaxLots`). The strategy halves the working volume if this limit is exceeded. |
| **LossThresholdPips** | Loss threshold in MetaTrader points that triggers a volume doubling event (`LossPips`). |
| **ProfitThresholdPips** | Profit target in points that resets the volume back to the starting value (`ProfitPips`). |
| **TakeProfitPips** | Optional fixed take profit distance in points applied through the built-in protection helper (`TakeProfit`). |
| **UseMoneyManagement** | Enables or disables the martingale-like position sizing routine (`MM`). |

## Usage Notes

- Configure the strategy with the same symbol and time frame that were used in MetaTrader; the default period of 240 works well with H1 candles, replicating the original setup.
- The point thresholds assume that the instrument provides a valid `PriceStep` and `StepPrice`. For symbols that lack this metadata you may need to adjust the thresholds manually.
- Because the original code recalculates margins before every entry, the port performs a simplified volume normalization step that halves the trading size whenever it exceeds `MaxVolume`. Additional risk controls can be added via the standard StockSharp risk providers if necessary.
- Only completed candles trigger entries and exits, mirroring the MQL implementation that checked `Close[1]` and `Close[2]` values on each new bar.

## Files

- `CS/MovingAveragePositionSystemStrategy.cs` – C# implementation of the trading logic using the StockSharp high-level strategy API.
- `README.md` – English documentation (this file).
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.

