# Renko Live Chart Strategy

This strategy emulates a classic Renko brick chart and trades on brick direction changes. It was converted from the MetaTrader script **RenkoLiveChart_v600**.

## Logic

The strategy builds Renko bricks using finished time‑based candles. When the price moves by at least the selected box size from the last brick price, a new brick is formed. A long position is opened on an upward brick and a short position on a downward brick.

## Parameters

- **Candle Type** – timeframe of the input candles used for brick construction.
- **Brick Size** – price step that defines the height of a Renko brick.
- **Brick Offset** – initial offset in bricks applied to the first brick.
- **Show Wicks** – display wicks on the chart when drawing candles.

## Notes

- Trades are executed only on completed candles.
- Position protection is started automatically on strategy start.
- This implementation focuses on core Renko behaviour and ignores advanced features of the original script, such as external file handling.
