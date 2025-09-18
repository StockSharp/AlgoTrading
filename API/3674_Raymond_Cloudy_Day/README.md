# Raymond Cloudy Day Strategy

## Overview
Raymond Cloudy Day is a breakout-following strategy that reconstructs the trading logic of the original **"Raymond Cloudy Day for EA"** MQL5 expert advisor. The algorithm derives a set of reference levels from a higher timeframe candle and uses them to detect momentum resumption on the execution timeframe. The StockSharp port keeps the original trading rules while exposing each component as configurable strategy parameters.

## Market Data
- **Signal candles** – the timeframe on which trades are executed. The strategy subscribes to this series for entry signals and position management.
- **Pivot candles** – the higher timeframe used to compute Raymond levels. By default this is a daily candle, reproducing the MQL5 input `RayMondTimeframe`.

Both subscriptions are automatically registered through `GetWorkingSecurities`, so the strategy requests the required data streams as soon as it is started.

## Raymond Level Calculation
For every finished pivot candle the strategy stores the four core levels defined by the original EA:

\[
\begin{aligned}
TradeSS &= \frac{High + Low + Open + Close}{4} \\
PivotRange &= High - Low \\
ETB &= TradeSS + 0.382 \times PivotRange \\
ETS &= TradeSS - 0.382 \times PivotRange \\
TPB1 &= TradeSS + 0.618 \times PivotRange \\
TPS1 &= TradeSS - 0.618 \times PivotRange \\
TPB2 &= TradeSS + PivotRange \\
TPS2 &= TradeSS - PivotRange
\end{aligned}
\]

The StockSharp implementation maintains the most recent snapshot of these values and logs every update, allowing the user to monitor how levels evolve over time.

## Entry Logic
Once the Raymond levels are available, the strategy evaluates each finished signal candle:

1. **Long setup** – If the candle’s low dips below `TPS1` and the close returns above the level, the strategy enters a long position. This mirrors the EA condition `Low[1] < TPS1 && Close[1] > TPS1` and captures bullish rejection of the level.
2. **Short setup** – If the candle remains fully above `TPS1` but closes below it, the strategy opens a short position (matching the original albeit asymmetric rule).

Before placing a new order the algorithm cancels any outstanding orders and, if necessary, closes the opposite position so that only one directional trade remains active.

## Risk Management
Raymond Cloudy Day uses symmetric protective offsets measured in ticks:

- **Stop-loss** – positioned `ProtectiveOffsetTicks` below the long entry (or above the short entry).
- **Take-profit** – positioned `ProtectiveOffsetTicks` above the long entry (or below the short entry).

The offsets are multiplied by the instrument’s `PriceStep` to convert ticks into absolute price distances. Each completed signal candle triggers a check that closes the position when either protective level is hit. When the strategy is flat the internal protection state is reset to avoid stale levels.

## Parameters
| Name | Description | Default | Notes |
|------|-------------|---------|-------|
| `TradeVolume` | Order volume used for every entry. | `1` | Synchronized with the `Volume` property on start. |
| `ProtectiveOffsetTicks` | Distance in ticks for both stop-loss and take-profit. | `500` | Multiplied by `PriceStep` to obtain absolute prices. |
| `SignalCandleType` | Candle type that produces trade signals. | `1 hour` time frame | May be set to any `DataType` representing candles. |
| `PivotCandleType` | Higher timeframe for Raymond level calculations. | `1 day` time frame | Matches the `RayMondTimeframe` input from the MQL EA. |

All parameters support optimization ranges and descriptive metadata for StockSharp Designer.

## Additional Notes
- The strategy requires `PriceStep` to be defined by the connected security. If it is missing, trade entries are skipped and a warning is logged.
- Chart visualization adds the execution candles together with executed trades. Additional custom drawing can be added if desired.
- The implementation avoids direct indicator value polling and processes only finished candles, adhering to the project guidelines in `AGENTS.md`.

## Original EA Specifics Preserved
- Raymond level formulas and multipliers (`0.382`, `0.618`, `1.0`).
- Entry logic based on the first sell take-profit (`TPS1`).
- Symmetric 500-point stop-loss and take-profit offsets converted to ticks in the StockSharp environment.

With these components the StockSharp strategy behaves identically to the source EA while providing rich configuration and logging suitable for further research and automation.

