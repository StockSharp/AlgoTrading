# Personal Assistant MNS Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the MetaTrader `personal_assistant_codeBase_MNS` expert advisor to StockSharp. It acts as a manual trading helper: instead of generating autonomous signals, it exposes C# methods that replicate the hotkey-driven actions from the original EA (open/close trades, adjust volume, or liquidate profitable positions). The helper also logs informative metrics about the symbol, active orders, and currently configured risk levels on every finished candle.

## How it works

1. The strategy subscribes to a configurable candle series (`CandleType`, 1 minute by default).
2. Each finished candle triggers an update that prints: current position, PnL, number of active stop/take orders, spread, tick value, and the configured magic number.
3. Manual commands (e.g., `PressBuy()` or `PressSell()`) submit market orders with the current helper volume. Optional stop-loss and take-profit levels are translated from pip distances and cached inside the strategy.
4. Protective levels are emulated on candle data: if price touches the cached stop or target, the strategy issues market exits.
5. An optional move-to-break-even rule (`UseTrailingStop`) arms after the price advances by `BreakEvenTriggerPips`; once armed, it liquidates the position if price retreats to the entry price plus `BreakEvenOffsetPips`.

## Features

- Replicates buttons 1–8 from the MQL assistant via public methods:
  - `PressBuy()` / `PressSell()` – open market trades with optional protective levels.
  - `PressCloseAll()` – flatten all exposure.
  - `IncreaseVolume()` / `DecreaseVolume()` – adjust the helper volume in 0.01 lots.
  - `CloseLongPositions()` / `CloseShortPositions()` – close one side only.
  - `CloseProfitablePositions()` – close the position when floating PnL is positive.
- Logs a detailed action legend on start when `DisplayLegend` is enabled.
- Converts pip-based risk distances into absolute prices using the instrument's price step and decimal precision.
- Supports break-even trailing for both long and short positions, mirroring the original `MOVETOBREAKEVEN()` routine.
- Keeps independent cached stop/take levels for long and short trades so that switching direction discards obsolete levels automatically.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `MagicNumber` | Informational identifier copied from the MQL `MagicNo` input. |
| `DisplayLegend` | Enable to print the control legend and candle-by-candle status messages. |
| `OrderVolume` | Base market order volume (lots) reused by all manual actions. |
| `Slippage` | Maximum tolerated slippage (in ticks), stored for reference. |
| `TakeProfitPips` | Pip distance for the cached take-profit level (0 disables it). |
| `StopLossPips` | Pip distance for the cached stop-loss level (0 disables it). |
| `UseTrailingStop` | Enable or disable the break-even trailing logic. |
| `BreakEvenTriggerPips` | Profit distance (in pips) required before the break-even stop arms. |
| `BreakEvenOffsetPips` | Offset (in pips) added to the entry price once the stop is armed. |
| `CandleType` | Candle series used for monitoring and level emulation. |

## Usage tips

- Call the helper methods from Designer actions, scripts, or UI controls to imitate key presses from the original MetaTrader panel.
- Protective levels and break-even distances rely on the instrument providing `PriceStep`, `StepPrice`, and `Decimals`. For exotic instruments without this metadata, adjust the pip distances manually or disable the features by setting them to `0`.
- Because stop/take levels are reproduced using candle highs and lows, very fast intrabar spikes may not be captured unless the candle timeframe is small. Reduce the timeframe if finer granularity is required.
- `CloseProfitablePositions()` mirrors the "button 8" behavior: it checks the floating PnL and closes the entire position only when the value is strictly positive.

## Differences from the MetaTrader version

- Chart labels are replaced with log entries because StockSharp does not expose the same drawing primitives within strategies.
- Stop-loss and take-profit orders are simulated through market exits on candle events instead of immediate pending orders.
- Break-even management is implemented with StockSharp market orders; it does not modify existing protective orders.
- Slippage is kept as an informational parameter; actual execution is handled by the StockSharp connector.
