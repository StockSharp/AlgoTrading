# Alligator Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy reproduces the classic Bill Williams Alligator system from the original MetaTrader script (`Alligator.mq5`). It uses three smoothed moving averages built on the median price and shifted forward to visualize the market phase. A long position is opened when the fast Lips line is above the Teeth, and the Teeth are above the Jaw. A short position is opened when the alignment is reversed. Only one position can be active at the same time.

Once in a trade the strategy protects the position with a stop-loss and take-profit expressed in pips. When the market moves in favour of the trade by a configurable zero-level distance, the stop is moved to break-even. A trailing stop follows the highest high (for longs) or lowest low (for shorts) with a minimum step to avoid frequent stop updates. Positions are closed when either the stop-loss, the trailing stop, or the take-profit levels are reached.

The default configuration targets 30-minute candles and Forex-style pip values, but the parameters can be optimized for other markets. Because the original MQL version uses broker-specific pip handling, the conversion relies on the instrument `PriceStep` to translate pip distances into absolute prices.

## Trading Rules

### Entry
- **Long**: No open position and `Lips > Teeth > Jaw` on the last completed candle.
- **Short**: No open position and `Lips < Teeth < Jaw` on the last completed candle.

### Exit and Risk Management
- **Initial Stop**: Placed `StopLossPips` below (long) or above (short) the fill price.
- **Take Profit**: Placed `TakeProfitPips` away from the fill price.
- **Zero Level**: When the price advances by `ZeroLevelPips`, the stop is moved to the entry price.
- **Trailing Stop**: After the zero-level activation, the stop trails the extreme by `TrailingStopPips`, updating only when the improvement exceeds `TrailingStepPips`.
- Positions are flattened immediately when any stop or the take-profit level is touched on candle data.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 30-minute time frame | Candle series used for indicator calculations and signal evaluation. |
| `JawLength` | 13 | Smoothed moving average period for the blue jaw line. |
| `TeethLength` | 8 | Smoothed moving average period for the red teeth line. |
| `LipsLength` | 5 | Smoothed moving average period for the green lips line. |
| `JawShift` | 8 | Forward displacement of the jaw line, expressed in bars. |
| `TeethShift` | 5 | Forward displacement of the teeth line, expressed in bars. |
| `LipsShift` | 3 | Forward displacement of the lips line, expressed in bars. |
| `EnableLong` | `true` | Allows or blocks long entries. |
| `EnableShort` | `true` | Allows or blocks short entries. |
| `StopLossPips` | 45 | Stop-loss distance in pips from the fill price. |
| `TakeProfitPips` | 145 | Take-profit distance in pips from the fill price. |
| `ZeroLevelPips` | 30 | Distance in pips required to move the stop to break-even. |
| `TrailingStopPips` | 50 | Distance between the current extreme and the trailing stop. |
| `TrailingStepPips` | 10 | Minimum pip improvement required before updating the trailing stop. |

## Notes

- The Alligator indicator is calculated on the median price `(High + Low) / 2` to match the MetaTrader implementation.
- Shifted line values are emulated with internal buffers so that comparisons use the same displaced data as the original script.
- The strategy assumes that one trade is filled before a new signal is processed on the same bar, mirroring the bar-by-bar execution of the source EA.
- Optimize pip distances to match the tick size and volatility of the traded instrument.
