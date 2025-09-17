# Breakthrough Volatility Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakthrough Volatility Strategy searches for short bursts in intrabar volatility. It waits for a candle whose range expands beyond the previous candle but only by a narrow band (two pip-equivalents after digit normalization). When such a candle closes bullish, the strategy buys; when it closes bearish, it sells. Protective stops, an optional trailing stop, and an automatic reverse-on-loss sequence manage risk and attempt to recover from adverse moves.

## Trading Logic

1. **Range Expansion Filter**
   - Calculate the current candle range (`High - Low`) and compare it with the previous candle.
   - Require the current range to be larger, yet not exceed the previous range by more than two normalized pips.
   - This creates a setup where volatility is increasing but still constrained, pointing to a potential breakout without excessive noise.
2. **Directional Bias**
   - If the candle closes above its open, enter long.
   - If the candle closes below its open, enter short.
   - The strategy can optionally forbid more than one entry per bar to avoid repeated signals on the same candle.
3. **Position Management**
   - Initial stop-loss and take-profit are assigned in points (pip equivalents) relative to the entry price.
   - An optional trailing stop tightens the protective level once price has moved a specified distance in favor of the trade. A trailing step prevents tiny adjustments.
   - When a position closes with a loss, the strategy can reverse direction immediately. Each reverse increases the take-profit distance to compensate for the additional risk. A cap on the number of consecutive reverses prevents infinite martingale behaviour.

## Parameters

| Name | Description | Default | Optimizable |
| --- | --- | --- | --- |
| `TradeVolume` | Base order volume for market entries. | `0.1` | Yes |
| `StopLossPoints` | Stop-loss distance in points. | `20` | Yes |
| `TakeProfitPoints` | Take-profit distance in points. | `10` | Yes |
| `TrailingStopPoints` | Trailing stop distance in points. Set to `0` to disable. | `25` | No |
| `TrailingStepPoints` | Minimum incremental step when moving the trailing stop. | `5` | No |
| `OnlyOnePositionPerBar` | Forbid multiple entries during the same candle. | `true` | No |
| `UseAutoDigits` | Multiply the point size by 10 for symbols with 3 or 5 decimals to convert to pip units. | `true` | No |
| `ReverseAfterStop` | Enable the reverse-on-loss workflow. | `true` | No |
| `MaxReverseOrders` | Maximum number of consecutive reverse trades. | `2` | No |
| `TakeProfitIncrease` | Extra take-profit points added for each reverse order. | `100` | No |
| `CandleType` | Candle type and timeframe for calculations. | `TimeSpan.FromMinutes(1)` | No |

## Risk Management

- Stop-loss and take-profit offsets are recalculated using the instrument price step. Digit auto-detection converts five-digit quotes to pip-sized distances.
- Trailing logic only activates after the market advances by the specified trailing distance and enforces a minimum step before modifying the stop.
- Reverse trading resets after a profitable exit or after hitting the configured limit of consecutive reverses.

## Practical Notes

- Works best on currency pairs with tight spreads, where small volatility changes can indicate momentum bursts.
- Consider aligning the candle timeframe with the target market session; the default 1-minute timeframe captures high-frequency breakouts.
- Because reversals are executed immediately after a losing close, ensure sufficient margin is available for back-to-back trades.
