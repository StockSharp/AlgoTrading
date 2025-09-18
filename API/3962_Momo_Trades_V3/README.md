# Momo Trades V3 Strategy

## Overview
Momo Trades V3 is a momentum strategy converted from the original MetaTrader expert advisor. It combines a multi-condition MACD pattern detector with a displaced exponential moving average (EMA) filter. The StockSharp port keeps the discretionary elements of the EA, adds optional breakeven handling, and provides a risk-based position sizing mode that mirrors the automatic lot logic of the script.

## Trading Logic
1. **MACD momentum patterns** – the strategy watches the main MACD line using the classic `(12, 26, 9)` parameters and an additional displacement (`MacdShift`). Two bullish patterns are accepted:
   - A strictly rising sequence where the third value equals zero and the subsequent two samples continue to rise.
   - A sequence where the MACD crosses above zero, with the following samples remaining positive while previous values are negative.
   Bearish entries require the mirrored conditions with decreasing values and the line crossing below zero.
2. **EMA distance filter** – the closing price of the shifted bar (`MaShift`) must be at least `PriceShiftPoints` MetaTrader points above the EMA for long trades and below the EMA for shorts. This avoids entries when price hugs the average.
3. **Single position regime** – the strategy opens a new position only when it is flat. Opposite signals are ignored while a trade is active.
4. **Session close exit** – when `CloseEndDay` is enabled, the strategy liquidates any position at 23:00 platform time (21:00 on Fridays) to avoid overnight exposure.
5. **Optional breakeven stop** – when `UseBreakeven` is on, once price moves far enough to place a stop at the entry price plus `BreakevenOffsetPoints`, the strategy arms a breakeven level. If price then returns to or beyond that level, the position is closed at market.

## Risk Management
- **Initial protection** – `StopLossPoints` and `TakeProfitPoints` are converted into absolute price distances through the instrument price step and passed to `StartProtection`, so protective orders are attached automatically.
- **Auto volume** – if `UseAutoVolume` is true, the order size is calculated from the current portfolio equity. The strategy allocates `RiskFraction` of equity to the trade, divides by the contract value (`price × lot size`), normalises the result to the exchange volume step, and respects `VolumeMin`/`VolumeMax` bounds. When auto sizing is disabled, `TradeVolume` is used directly.

## Indicators
- **Moving Average Convergence Divergence (MACD)** – delivers the main momentum signal and is evaluated on historical samples using `MacdShift`.
- **Exponential Moving Average (EMA)** – used as a displaced trend filter.

## Parameters
| Name | Type | Default | Description |
|------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeFrame(15m)` | Primary timeframe used for signal generation. |
| `MaPeriod` | `int` | `22` | EMA period for the displacement filter. |
| `MaShift` | `int` | `1` | Number of completed bars used when sampling the close price and EMA. |
| `FastPeriod` | `int` | `12` | Fast EMA length for MACD. |
| `SlowPeriod` | `int` | `26` | Slow EMA length for MACD. |
| `SignalPeriod` | `int` | `9` | Signal EMA length for MACD. |
| `MacdShift` | `int` | `1` | Additional displacement applied when evaluating the MACD patterns. |
| `PriceShiftPoints` | `decimal` | `10` | Minimum distance (in MetaTrader points) between the shifted close and the EMA required to open a position. |
| `TradeVolume` | `decimal` | `0.1` | Default trading volume when auto sizing is disabled. |
| `RiskFraction` | `decimal` | `0.1` | Fraction of portfolio equity used to size the order when `UseAutoVolume` is true. |
| `UseAutoVolume` | `bool` | `false` | Enables risk-based volume sizing. |
| `StopLossPoints` | `decimal` | `100` | Initial stop-loss distance expressed in MetaTrader points. `0` disables the protective stop. |
| `TakeProfitPoints` | `decimal` | `0` | Initial take-profit distance in MetaTrader points. `0` disables the target. |
| `CloseEndDay` | `bool` | `true` | Closes open positions near the end of the trading day (23:00, or 21:00 on Fridays). |
| `UseBreakeven` | `bool` | `false` | Activates the breakeven management logic. |
| `BreakevenOffsetPoints` | `decimal` | `0` | Offset added to the entry price when arming the breakeven exit. |

## Usage Notes
- Ensure the instrument has a valid `PriceStep`; otherwise the strategy falls back to a `0.0001` point value when converting MetaTrader points to price distances.
- The MACD filter relies on finished candles; the strategy exits early for unfinished bars to match the original EA behaviour.
- Because only one position is allowed at a time, risk per trade remains controlled by the single `TradeVolume` (or the auto-sized equivalent).
