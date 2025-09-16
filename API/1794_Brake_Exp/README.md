# Brake Exp Strategy

This strategy trades based on the **BrakeExp** indicator. The indicator draws an adaptive support and resistance channel built from an exponential curve. A switch of the channel from lower to upper line generates a sell signal, and a switch from upper to lower line generates a buy signal.

## How it works

- When the indicator reports an **up signal**, the strategy closes short positions and opens a new long position.
- When a **down signal** appears, existing long positions are closed and a short position is opened.
- If only an **up trend** is detected the strategy exits short positions.
- If only a **down trend** is detected the strategy exits long positions.

Signals are processed on finished candles only.

## Parameters

- `A` – curve acceleration factor of the BrakeExp indicator.
- `B` – price step used for the channel width.
- `CandleType` – candle series for indicator calculation.
- `Volume` – order volume used when entering the market.

## Notes

The strategy uses the high level StockSharp API and can be run in Designer, Shell or any other StockSharp product.
