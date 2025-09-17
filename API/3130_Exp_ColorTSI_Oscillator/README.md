# Exp Color TSI Oscillator Strategy

## Overview
- Conversion of the MetaTrader 5 expert advisor **Exp_ColorTSI-Oscillator** into the StockSharp framework.
- Reconstructs the ColorTSI oscillator: a double-smoothed True Strength Index with a delayed trigger line and multiple smoothing algorithms taken from `SmoothAlgorithms.mqh`.
- Generates trades when the oscillator turns up or down relative to its lagged trigger, replicating the "swing reversal" style used by the original EA.

## Indicator reconstruction
- Applied price is selected through the `ColorTsiAppliedPrice` option (close, open, median, typical, weighted, Demark, etc.).
- Price momentum (`diff = price[n] - price[n-1]`) and its absolute value are smoothed in two stages:
  1. **First stage**: configurable `ColorTsiSmoothingMethod` (`Sma`, `Ema`, `Smma`, `Lwma`, `Jjma`, `Jurx`, `Parma`, `T3`, `Vidya`, `Ama`) with length `FirstLength` and phase `FirstPhase` for Jurik-like filters.
  2. **Second stage**: identical method options with `SecondLength`/`SecondPhase` applied to the already-smoothed momentum series.
- The oscillator output is `TSI = 100 * smoothMomentum / smoothAbsMomentum`. When the denominator is zero, the value is ignored.
- A trigger line is obtained by delaying the TSI by `TriggerShift` bars, mirroring the MetaTrader buffer logic.
- Historical values are stored so that `SignalBar` matches MetaTrader's `CopyBuffer` access pattern (index `SignalBar` = most recent closed bar examined, `SignalBar + 1` = previous bar, etc.).

## Trading rules
- Calculations run on finished candles supplied by `CandleType` (default: 4-hour time frame).
- Let `TSI[k]` be the oscillator value and `Trigger[k]` be the delayed series.
- **Bullish context**: `TSI[SignalBar + 1] > Trigger[SignalBar + 1]` ⇒ the previous bar showed upward momentum.
  - Close shorts if `EnableShortExits` is true.
  - Open a long position when `EnableLongEntries` is true **and** `TSI[SignalBar] ≤ Trigger[SignalBar]`, signalling an upswing after the pullback.
- **Bearish context**: `TSI[SignalBar + 1] < Trigger[SignalBar + 1]` ⇒ the previous bar showed downward momentum.
  - Close longs if `EnableLongExits` is true.
  - Open a short position when `EnableShortEntries` is true **and** `TSI[SignalBar] ≥ Trigger[SignalBar]`.
- Entry signals are keyed by the time of the analysed bar plus one full timeframe; each signal can trigger at most one trade thanks to `_lastLongEntryTime` / `_lastShortEntryTime` guards.
- All actions are executed with market orders. Existing opposite positions are closed before reversals.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Data stream used for analysis. Supports any `DataType` (time, tick, volume candles). | H4 time frame |
| `Volume` | Fixed order size replacing the EA's money-management blocks. Must be > 0. | 0.1 |
| `FirstMethod`, `FirstLength`, `FirstPhase` | First smoothing stage for momentum and absolute momentum. | SMA, 12, 15 |
| `SecondMethod`, `SecondLength`, `SecondPhase` | Second smoothing stage. | SMA, 12, 15 |
| `PriceMode` | Applied price option feeding the oscillator. | Close |
| `SignalBar` | Bar shift used for evaluating signals (1 = last closed bar). | 1 |
| `TriggerShift` | Delay applied to the trigger line (1 reproduces original indicator). | 1 |
| `EnableLongEntries` / `EnableShortEntries` | Allow opening long/short trades. | true |
| `EnableLongExits` / `EnableShortExits` | Allow closing positions on opposite context. | true |
| `StopLossPoints` | Stop-loss distance in price points (converted with instrument `PriceStep`). | 1000 |
| `TakeProfitPoints` | Take-profit distance in price points. | 2000 |

## Risk management
- The original EA relied on helper functions from `TradeAlgorithms.mqh` for SL/TP placement. The C# version calls `StartProtection` with the selected distances converted to `UnitTypes.Point`.
- If either distance is set to 0, the corresponding protective order is omitted.
- No trailing stops or position scaling are implemented; these match the MetaTrader behaviour for this expert.

## Differences from the MetaTrader version
- Margin-based lot sizing (`MM` and `MMMode`) is replaced by a fixed `Volume` parameter. This keeps the behaviour deterministic across brokers and avoids replicating account-specific leverage logic.
- Slippage (`Deviation_`) is not emulated because StockSharp market orders do not expose a slippage parameter.
- Indicator smoothing is fully reconstructed using StockSharp indicators (including Jurik phase handling through reflection), so signal values are consistent with the original buffers.
- Python implementation is intentionally omitted as requested.

## Usage notes
- Ensure the selected security provides the candle type requested by `CandleType`. For standard timeframes use `TimeSpan.FromHours(x).TimeFrame()`.
- `SignalBar` must be ≥ `TriggerShift` to obtain valid trigger values; otherwise signals are skipped until enough history accumulates.
- Because the strategy reacts on finished candles, enable real-time order registration only after `IsFormedAndOnlineAndAllowTrading()` becomes true.
- The chart area visualises price candles and executed trades; indicators are reconstructed internally and are not auto-plotted.
- To reproduce the MetaTrader defaults: keep all smoothing settings at SMA with length 12, keep both entry and exit toggles enabled, and use the default stop/take distances.
