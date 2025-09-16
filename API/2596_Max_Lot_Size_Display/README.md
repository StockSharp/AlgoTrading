# Max Lot Size Display Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 5 indicator **EXP_MAX_LOT**. The original script displayed the largest position size that could be opened with the currently available margin. The StockSharp version keeps the same purpose: it continuously estimates the maximum order volume that can be placed in the selected direction without exceeding free funds.

Unlike classic trading systems, the strategy does not send orders. It acts as a risk management helper and updates the calculated value on every finished candle of the configured timeframe. The result is written to the log and stored in the `Volume` property, so other automated components can reuse the information.

## Parameters
- **Trade Direction** – side (`Buy` or `Sell`) for which the maximum volume is evaluated. Mirrors the original `PosType` input.
- **Risk Fraction** – multiplier applied to free equity before the volume calculation. Equivalent to the `Money_Management` argument of the MQL script. Value `1.0` uses the whole free margin, while lower values reserve part of it.
- **Label Prefix** – text prefix added to informational log messages. Preserves the indicator label style (`MAX_LOT_Label_`).
- **Candle Type** – candle data source that triggers recalculation. The original code updated on each tick; here we refresh after every completed candle of the selected type to match the high-level API workflow.

## Logic
1. Subscribe to candle data defined by **Candle Type** and wait for finished candles.
2. On each update:
   - Obtain portfolio equity (current value or starting value if current is unavailable).
   - Multiply equity by **Risk Fraction** to determine available funds.
   - Compute the required margin per unit for the chosen **Trade Direction**. If the exchange provides margin figures (`MarginBuy`/`MarginSell`), they are used directly; otherwise an estimate based on the latest candle close and the security `VolumeStep` is applied.
   - Convert the raw result into valid exchange volume by snapping to `VolumeStep` and applying `MinVolume`/`MaxVolume` limits.
3. Store the final figure in `Volume` and write a log line. When no sufficient funds exist, a warning is logged instead.

The logic mirrors the helper functions from the original `.mq5` file (`LotCount`, `LotCorrect`, `LotFreeMarginCorrect` and `GetLotForOpeningPos`) while adapting them to StockSharp entity properties.

## Implementation Notes
- Only finished candles are processed to respect high-level strategy conventions.
- Tab indentation and English inline comments follow repository requirements.
- If both margin and price information are missing, the strategy returns zero and issues a warning.
- The strategy calls `StartProtection()` once during startup to keep the standard defensive pattern used across samples.

## Usage
Attach the strategy to an instrument and set the parameters according to your risk profile. Monitor the log output for messages similar to:

```
MAX_LOT_Label_: Maximum volume for Buy = 2.5
```

This value can also be read from the `Volume` property of the strategy to automate lot sizing in other components.
