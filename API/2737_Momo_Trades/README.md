# Momo Trades Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Conversion of the original MetaTrader "Momo_trades" expert advisor that trades momentum breakouts filtered by a moving average and MACD structure.

## Strategy logic
- Works on finished candles from the configured timeframe and processes only one net position at a time.
- Uses a simple moving average with a configurable bar shift to measure how far price closed away from the average. Long trades require the shifted close to be above the SMA by more than the price shift threshold, shorts require the opposite.
- Evaluates a cascaded MACD momentum pattern that mirrors the MQL rules: several past MACD main-line values must increase through zero for longs or decrease through zero for shorts. This prevents trades while momentum is fading.
- Opens a market order with the strategy volume once both the SMA distance filter and MACD pattern align for the same direction.

## Risk management
- Stop loss, take profit, trailing stop, trailing step, breakeven and price shift inputs are defined in pips and automatically converted to price units using the instrument step.
- When take profit and trailing values are provided the stop is trailed only after price advances by the trailing distance plus the trailing step, reproducing the MQL behaviour.
- When no take profit is configured but a breakeven distance is set, the stop is moved to the entry price once the breakeven trigger is reached.
- All stop and take levels are recalculated every finished candle and are closed by market orders when crossed by candle extremes.

## Session management
- The `CloseEndDay` flag matches the original expert advisor and closes any active position at 23:00 platform time (21:00 on Fridays). After the cut-off the strategy skips new entries until the next day.

## Parameters
- **SMA Period / MA Bar Shift** – moving average length and the bar index used to obtain SMA and price values.
- **MACD Fast / Slow / Signal / Bar Shift** – MACD configuration and the offset applied to the stored values for the pattern checks.
- **Stop Loss / Take Profit / Trailing Stop / Trailing Step / Breakeven / Price Shift** – pip distances that control exit, trailing and SMA filters.
- **Close End Of Day** – closes positions after the configured session end.
- **Candle Type** – time frame used for candles and indicator calculations.
