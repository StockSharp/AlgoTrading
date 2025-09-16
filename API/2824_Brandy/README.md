# Brandy Strategy (C#)
[中文](README_cn.md) | [Русский](README_ru.md)

## Overview
The Brandy strategy is a direct port of the MetaTrader 5 Expert Advisor *Brandy (barabashkakvn's edition)*. It combines two configurable moving averages and evaluates their relative positions on closed candles to decide whether to open a long or short position. The original logic also enforces optional stop loss, take profit and trailing stop controls expressed in pips. This C# version faithfully reproduces those behaviours on top of the StockSharp high-level strategy API.

The strategy calculates a “fast” moving average on the open price stream and a “slow” moving average on the close price stream. Both indicators have independent period, smoothing method, price source, signal-bar reference and shift parameters. Signals are generated when the previous bar's MA values are on the same side of the respective signal values. Protective logic checks the open-based moving average every candle and immediately exits the trade if the trend condition is no longer satisfied. Additional risk management is implemented with optional stop loss, take profit and trailing stop distances, all measured in pips and converted to absolute prices by using the instrument tick size with a five-digit pip adjustment.

## Trading Logic
1. On every finished candle the strategy updates the open-price and close-price moving averages using the configured smoothing method and applied price. Historical MA values are buffered so the code can emulate the `iMA` shifting behaviour from the original Expert Advisor.
2. When there is no active position, a long trade is opened if:
   - The previous open-based MA value is greater than the configured signal value (possibly shifted);
   - The previous close-based MA value is also greater than its signal reference (note that the original EA compares against the open-based indicator for this check, and the port keeps that quirk for compatibility).
3. A short trade is opened when both moving averages are below their respective signal references.
4. While a position is active the strategy evaluates exits on every finished candle in the following order:
   - Trend reversal: if the previous open-based MA drops below the signal value (for longs) or rises above it (for shorts), the position is closed immediately at market.
   - Trailing stop update: when enabled and the move in favour of the trade exceeds *trailing stop + trailing step* (converted to absolute prices), the stop level is tightened to maintain a distance of *trailing stop* from the latest close.
   - Take profit: if the candle range touches the profit target, the trade is exited at market.
   - Stop loss: if the candle range breaches the protective stop level, the trade is closed.
5. All volume is fixed and determined by the `TradeVolume` parameter. The default value replicates the 0.1 lot setting from the MT5 version.

## Parameter Reference
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Market order size in lots.
| `StopLossPips` | Distance of the protective stop, measured in pips (0 disables it).
| `TakeProfitPips` | Distance of the profit target in pips (0 disables it).
| `TrailingStopPips` | Trailing stop distance in pips. Requires `TrailingStepPips` to be positive.
| `TrailingStepPips` | Additional pip move required before the trailing stop is advanced. Must be non-zero when the trailing stop is active.
| `MaClosePeriod`, `MaOpenPeriod` | Moving average lengths for the close and open series respectively.
| `MaCloseShift`, `MaOpenShift` | Forward shifts applied to the MA buffers (number of bars).
| `MaCloseSignalBar`, `MaOpenSignalBar` | Bar indices used as comparison references. Zero matches the most recent value, one refers to the previous bar, and so on.
| `MaCloseMethod`, `MaOpenMethod` | Moving average smoothing methods (SMA, EMA, SMMA, LWMA).
| `MaCloseAppliedPrice`, `MaOpenAppliedPrice` | Candle price source for each indicator (close, open, high, low, median, typical, weighted).
| `CandleType` | Time frame of candles requested from the data source.

## Implementation Notes
- Pip size is computed from `Security.PriceStep` and multiplied by 10 whenever the instrument exposes 3 or 5 decimal places, mirroring the MetaTrader adjustment between points and pips.
- Indicator history is retained using bounded queues so the strategy can reproduce `iMA` calls with arbitrary signal-bar indexes and positive shifts without relying on forbidden indicator accessors.
- The closing condition for the close-based moving average intentionally compares against the **open** MA buffer because the original source code invoked `iMAGet(handle_iMAOpen, MaClose_SignalBar)`. This port keeps the behaviour to maintain compatibility with legacy configurations.
- Stops and trailing logic are executed on finished candles and approximate the order modifications performed by the Expert Advisor while respecting the StockSharp high-level API.

## Usage Tips
- Configure the `CandleType` parameter to match the timeframe used by the original EA (typically a single instrument timeframe).
- Keep `TrailingStopPips` at zero if no trailing behaviour is desired; otherwise ensure `TrailingStepPips` is strictly positive to avoid the initialization error enforced by the strategy.
- When back-testing in StockSharp, make sure the instrument’s `PriceStep` and `Decimals` reflect the intended pip definition so that risk distances are converted correctly.
