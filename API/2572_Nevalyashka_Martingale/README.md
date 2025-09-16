# Nevalyashka Martingale Strategy

## Overview
The Nevalyashka Martingale strategy is a direct port of the MetaTrader 5 expert advisor "Nevalyashka3_1". It runs a single-symbol martingale that alternates between buying and selling after losing trades. The strategy always starts by selling and measures account equity to decide whether the previous trade cycle ended in profit or loss. Profit resets the volume back to the base lot size and keeps the direction unchanged, while a loss multiplies the lot size and flips the direction in an attempt to recover the drawdown.

## How It Works
- **Initial trade** – a short position is opened on the first completed candle using the base lot size.
- **Equity tracking** – the strategy stores the highest observed equity value. When no position is open it compares the current equity with the stored peak.
  - If equity made a new high, the next trade uses the base lot size and repeats the last direction.
  - If equity failed to make a new high, the next trade increases the lot by the multiplier and switches direction.
- **Stop loss / take profit** – every order uses fixed distances defined in "points" (instrument steps). Take profit mirrors the original expert: the stop sits `StopLossPoints` away from entry and the target is `TakeProfitPoints` away.
- **Trailing** – once price moves by `MoveProfitPoints`, the stop is tightened. Each move requires an additional `MoveStepPoints` buffer so that the stop only advances when the market pushes further. When the stop is pulled beyond the entry price the planned volume is divided by the multiplier, scaling the next trade back down toward the base lot.
- **Position exit** – the position closes immediately when the candle high/low reaches the stop or take levels. After closing the strategy evaluates equity and prepares the next signal.

## Parameters
- `BaseVolume` – lot size for the initial trade and any profitable cycle (default 0.1).
- `VolumeMultiplier` – factor applied after a loss to increase the next lot (default 1.1).
- `TakeProfitPoints` – take-profit distance measured in price points (default 94).
- `MoveProfitPoints` – minimum favorable excursion before the trailing stop activates (default 25).
- `MoveStepPoints` – extra buffer required between successive trailing adjustments (default 11).
- `StopLossPoints` – initial stop-loss distance measured in price points (default 70).
- `CandleType` – timeframe used for trade management. The default is 5-minute candles.

## Position Management Details
- The strategy keeps `_plannedVolume` to mirror the original "Lot" variable. It only changes after a trade is closed or the stop moves past break-even.
- `AdjustVolume` respects exchange rules by aligning the lot size to `VolumeStep` and enforcing `MinVolume`/`MaxVolume`.
- `GetPointValue` replicates the MT5 "adjusted point" logic: for instruments quoted with 3 or 5 decimals the point size is multiplied by 10 to work with whole pips.
- `HandleLongPosition` and `HandleShortPosition` use candle highs and lows to emulate MT5 stop modification and exit behavior without relying on indicator history.

## Usage Notes
- The strategy assumes it trades a single security. Add it to the strategy and set `Security`/`Portfolio` before starting.
- Because it is a martingale, the risk grows quickly after a series of losses. Adjust `BaseVolume` and `VolumeMultiplier` carefully and test with realistic margin requirements.
- The stop and take-profit distances are defined in instrument points. Ensure the security metadata (`PriceStep`, `VolumeStep`, `MinVolume`) are populated so that the offsets and lot calculations match your broker.
- The trailing logic acts on finished candles. Intrabar stop hits may occur earlier in live trading depending on price path.
