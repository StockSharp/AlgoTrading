# Demarker Martingale Strategy (StockSharp)

## Overview
The **Demarker Martingale Strategy** recreates the MetaTrader expert advisor "Demarker Martingale" using the StockSharp high-level API. The system combines a medium-term DeMarker oscillator signal with a higher timeframe MACD trend filter. Entries are followed by martingale-style position sizing, hard stop-loss and take-profit levels, break-even protection, and a trailing stop that mimics the original expert's money management toolkit.

## Core Trading Logic
1. **Data feeds** – the strategy subscribes to a user-defined trading timeframe (default 15-minute candles) for signal generation and a higher timeframe series (default monthly candles) to calculate the MACD filter.
2. **DeMarker trigger** – when the DeMarker value exceeds the neutral `DemarkerThreshold` (default 0.5) and the recent price action forms a bullish overlap (`Low[2] < High[1]`), a long setup is considered. Conversely, a bearish overlap with DeMarker below the threshold prepares a short.
3. **MACD confirmation** – the higher timeframe MACD must agree with the direction. A bullish signal requires the MACD main line to be above its signal line, while a bearish signal expects the opposite relationship. This reproduces the MQL expert's monthly MACD filter.
4. **Order execution** – valid signals place market orders with the current martingale-adjusted volume. Only one directional position is maintained at a time.
5. **Position monitoring** – while a position is open, the strategy evaluates every finished candle to detect stop-loss, take-profit, break-even, or trailing-stop triggers. Breach events close the full position via market orders.

## Money Management
- **Initial sizing** – orders start with `InitialVolume` aligned to the instrument's `VolumeStep` and bounded by `VolumeMin`/`VolumeMax`.
- **Martingale escalation** – after a losing trade the next volume is either multiplied by `MartingaleMultiplier` (`DoubleLotSize = true`) or incremented by `LotIncrement`. Profitable trades reset the ladder to the base volume. The escalation depth is limited by `MaxMartingaleSteps` to prevent runaway exposure.
- **Stop-loss & take-profit** – distances are expressed in MetaTrader-style pips. The pip size automatically adapts to 3/5-digit Forex quotes, matching the original `ticksize` logic.
- **Break-even** – once unrealised profit reaches `BreakEvenTriggerPips`, the stop-loss is shifted to entry plus `BreakEvenOffsetPips` (long) or minus the offset (short).
- **Trailing stop** – profits beyond `TrailingStopPips` move an internal trailing threshold that tightens with every candle, replicating the EA's `TrailingStop` behaviour.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Trading timeframe used for DeMarker signals. |
| `MacdCandleType` | Higher timeframe used to compute the MACD trend filter. |
| `DemarkerPeriod` | DeMarker lookback period. |
| `DemarkerThreshold` | Neutral boundary between bullish and bearish setups. |
| `MacdFast` / `MacdSlow` / `MacdSignal` | MACD EMA lengths. |
| `InitialVolume` | Base order size before martingale adjustments. |
| `MartingaleMultiplier` | Multiplication factor when `DoubleLotSize` is enabled. |
| `LotIncrement` | Additive increase when doubling is disabled. |
| `DoubleLotSize` | Toggle between multiplicative and additive martingale. |
| `MaxMartingaleSteps` | Maximum number of consecutive escalations. |
| `StopLossPips` | Stop-loss distance in pips. |
| `TakeProfitPips` | Take-profit distance in pips. |
| `TrailingStopPips` | Trailing stop distance in pips. |
| `UseBreakEven` | Enable or disable break-even logic. |
| `BreakEvenTriggerPips` | Profit threshold (in pips) before shifting to break-even. |
| `BreakEvenOffsetPips` | Buffer applied to the break-even stop. |

## Conversion Notes
- The pip conversion mirrors the MQL EA (`ticksize == 0.00001` or `0.001` implies a 10x pip scale). This preserves consistent risk distances on 3/5-digit quotes.
- The MACD trend filter uses `MovingAverageConvergenceDivergenceSignal` with the original EMA lengths and processes a separate candle series to emulate the monthly chart logic.
- Martingale bookkeeping tracks weighted-average entry prices and realised PnL to decide whether the next trade should escalate or reset.
- All protective actions (stop-loss, take-profit, break-even, trailing) execute via market exits because the high-level API discourages direct order modifications under the `StartProtection` guard.

## Usage Tips
- Ensure the assigned security exposes `PriceStep`, `VolumeStep`, `VolumeMin`, and `VolumeMax` to align pip calculations and volume rounding with exchange constraints.
- Experiment with `MacdCandleType` (e.g., weekly candles) to fine-tune the trend filter for faster markets.
- When optimising, jointly adjust `DemarkerThreshold`, `TrailingStopPips`, and martingale parameters to keep drawdowns in check.
- Combine the strategy with portfolio-level risk controls or trading session filters when deploying live, as martingale sequences inherently increase exposure after losses.
