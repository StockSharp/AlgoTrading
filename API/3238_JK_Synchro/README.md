# JK Synchro Strategy

## Overview

The **JK Synchro Strategy** is a StockSharp port of the MetaTrader 5 expert advisor "JK synchro" (MQL ID 2415). The original robot counts how many of the most recent candles closed lower or higher than they opened and then opens a position in the direction that dominates. This port replicates the behaviour while adding strongly typed parameters, built-in risk management hooks, and rich logging through StockSharp.

## Trading Logic

1. Subscribe to the candle source defined by `CandleType` and wait for finished candles.
2. Maintain a sliding window of `AnalysisPeriod` candles. For each candle:
   - Increment the **bearish** counter when `Open > Close`.
   - Increment the **bullish** counter when `Open < Close`.
   - Ignore doji candles where `Open == Close`.
3. Once the window is filled, check dominance:
   - If bearish candles outnumber bullish ones, prepare a **long** entry.
   - If bullish candles outnumber bearish ones, prepare a **short** entry.
4. Before entering a trade the strategy verifies:
   - The strategy is online and allowed to trade (`IsFormedAndOnlineAndAllowTrading`).
   - The current hour lies between `StartHour` and `EndHour` (inclusive).
   - The cooldown defined by `PauseBetweenTradesSeconds` has elapsed since the last entry.
   - Adding another lot would keep the net exposure within `MaxPositions * OrderVolume`.
5. When a signal appears while holding an opposite position, the strategy first closes that position and waits for the next candle before potentially entering in the new direction.
6. Protective stop-loss, take-profit, and trailing stop levels are expressed in pips and automatically translated into price offsets based on the instrument tick size.

## Risk Management

- **Stop Loss / Take Profit**: Optional levels defined in pips. They are recalculated on every position change and checked on each finished candle.
- **Trailing Stop**: Activated when both `TrailingStopPips` and `TrailingStepPips` are positive. Once the trade moves in favour by at least `TrailingStop + TrailingStep`, the stop follows the price using the configured step.
- **Position Cap**: The absolute net position cannot exceed `MaxPositions * OrderVolume`.
- **Entry Pause**: The strategy records the timestamp of every fill and enforces a pause before opening another trade.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `OrderVolume` | 0.1 | Volume placed with every market order. |
| `MaxPositions` | 10 | Maximum number of lots allowed per direction. |
| `AnalysisPeriod` | 18 | Number of finished candles considered when counting bullish versus bearish moves. |
| `PauseBetweenTradesSeconds` | 540 | Cooldown in seconds after any entry before a new one can be opened. |
| `StartHour` | 3 | Start hour (inclusive) of the trading window, server time. |
| `EndHour` | 6 | End hour (inclusive) of the trading window, server time. |
| `StopLossPips` | 50 | Stop-loss distance expressed in pips. Set to 0 to disable. |
| `TakeProfitPips` | 150 | Take-profit distance in pips. Set to 0 to disable. |
| `TrailingStopPips` | 15 | Trailing stop distance in pips. Set to 0 to disable trailing. |
| `TrailingStepPips` | 5 | Extra distance in pips before the trailing stop is updated. Must be positive when trailing is enabled. |
| `CandleType` | 15-minute time frame | Candle source used for all calculations. |

## Implementation Notes

- High-level StockSharp API is used throughout (`SubscribeCandles`, `.Bind`, `BuyMarket`, `SellMarket`).
- Entry timestamps are captured inside `OnPositionChanged` to implement the pause logic exactly like the original EA, which waited a fixed amount of time after each entry.
- Pip size is derived from `Security.PriceStep` and `Security.Decimals`; for 3- or 5-digit instruments the multiplier is automatically adjusted.
- Exits are handled on closed candles by comparing the high/low with the calculated stop and target levels.
- Trailing stops mimic the MetaTrader logic: they start moving only after the profit exceeds `TrailingStop + TrailingStep` and never reverse.

## Usage Tips

1. Align `OrderVolume` and `MaxPositions` with the contract size of your broker to keep exposure under control.
2. Choose `AnalysisPeriod` according to the candle timeframe. Shorter time frames usually require larger windows to avoid noise.
3. Adjust the trading window to match the active hours of the instrument (e.g., European session for EUR-based pairs).
4. Backtest different combinations of stop, target, and trailing settings—the original EA often ran with either fixed targets or trailing stops depending on market conditions.

## Differences from the MQL Version

- The StockSharp port uses a net exposure model. When switching direction the existing position is closed first, whereas the MetaTrader version could keep hedged positions.
- Logging and parameter management leverage StockSharp facilities, making optimisation and UI integration easier.
- The trailing stop is evaluated on finished candles, which is consistent with other StockSharp strategy ports and avoids reacting to incomplete bars.

With these considerations the JK Synchro strategy can be traded, analysed, and optimised directly within the StockSharp ecosystem.
