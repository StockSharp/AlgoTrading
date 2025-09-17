# Practice Mod Strategy (ID 4085)

## Overview
- Port of the MetaTrader 4 expert **PracticeMod.mq4** that teaches discretionary order management.
- Replaces horizontal/vertical chart objects with filesystem command files so the workflow remains interactive inside StockSharp.
- Uses high-level subscriptions: candle polling keeps command processing deterministic, while level-1 updates manage exits and trailing stops.

## Trading workflow
1. **Entry** – create `<CommandDirectory>/<EntryFileName>` (default `entry.txt`) with a single price. If the price is above the current bid the strategy buys; if it is below the bid it sells. The command file is deleted immediately after it is read.
2. **Protection updates** – while a position is open, write the desired price into `<CommandDirectory>/<ModifyFileName>` (default `modify.txt`). A value above the bid becomes the new stop-loss for shorts or take-profit for longs. A value below the bid becomes the new take-profit for shorts or stop-loss for longs. Multiple lines can be provided; each line is processed sequentially.
3. **Manual liquidation** – placing `<CommandDirectory>/<CloseFileName>` (default `close.txt`) requests a full position close on the next data update, regardless of direction. The request is ignored if no position exists.
4. **Trailing stop** – when `TrailingStopPips` is positive, level-1 updates move the protective stop closer to price once the move exceeds the configured number of points. The algorithm mirrors the original EA: long stops ratchet up, short stops trail down, and manually moved levels are only tightened, never loosened.

All command files must contain decimal prices formatted with a dot (`1.2345`). The strategy automatically deletes every command after reading it, so each action should be requested only once.

## Parameters
- `Volume` – order volume for market entries and exits.
- `TakeProfitPips` – initial take-profit distance in instrument points applied after a new position opens.
- `StopLossPips` – initial stop-loss distance in points, also applied right after entry.
- `TrailingStopPips` – trailing distance in points; set to `0` to disable trailing.
- `CommandDirectory` – folder that stores the command files. It is created automatically at start if it does not exist.
- `EntryFileName` – file name for pending entries.
- `ModifyFileName` – file name for stop/take modifications.
- `CloseFileName` – file name that triggers a manual close.
- `CandleType` – candle aggregation used only to ensure periodic polling when market data is quiet.

## Behaviour details and differences
- Market orders (`BuyMarket` / `SellMarket`) are used for both entries and exits, matching the instant execution of the MT4 version.
- Protective levels are tracked internally; when a target or stop is reached the strategy closes the position at market rather than sending linked stop orders.
- File parsing is tolerant to blank lines and ignores malformed values with a warning in the log.
- The strategy ignores new entry requests while a position or active order exists, keeping the single-position limitation of the original script.
- Close commands and stop/take triggers set a flag so the strategy does not fire repeated exit orders while waiting for fills.
