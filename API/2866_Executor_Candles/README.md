# Executor Candles Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a direct conversion of the MetaTrader "Executor Candles" expert. It reacts to a rich set of bullish and bearish candlestick reversal patterns and can optionally confirm trades with a higher timeframe trend candle. All trade management logic – stop losses, take profits, and trailing stops – mirrors the original expert's behaviour measured in pips (price steps).

## How it works

- **Trend filter**: When `UseTrendFilter` is enabled the strategy looks at the most recent finished candle of `TrendCandleType`. Long setups are allowed only if that candle closed bullish, while short setups require a bearish close. With the filter disabled (default) only pattern logic is used.
- **Long patterns**: Hammer, bullish engulfing, piercing line, morning star, and morning doji star structures taken from the last three completed trading candles.
- **Short patterns**: Hanging man, bearish engulfing, dark cloud cover, evening star, and evening doji star confirmations.
- **Trade management**:
  - Separate stop-loss and take-profit distances for long and short positions expressed in pips (`StopLossBuyPips`, `TakeProfitBuyPips`, `StopLossSellPips`, `TakeProfitSellPips`).
  - Optional trailing stops for both directions controlled by `TrailingStopBuyPips`, `TrailingStopSellPips`, and the minimum shift `TrailingStepPips`. A trailing update is made only after price advances by the stop distance plus the trailing step, replicating the MetaTrader logic.
  - Orders are placed with `OrderVolume` lots and the current position is fully reversed by market orders when an exit condition triggers.

The strategy subscribes to the configured `CandleType` for trading signals and, if necessary, to `TrendCandleType` for the confirmation candle. It keeps an internal buffer of the last three finished trading candles to evaluate the multi-bar patterns without storing long histories.

## Parameters

- `CandleType` – timeframe used for detecting the candlestick patterns.
- `TrendCandleType` – higher timeframe candle used when the trend filter is active.
- `OrderVolume` – order size for market entries and exits.
- `StopLossBuyPips`, `TakeProfitBuyPips`, `TrailingStopBuyPips` – risk controls for long positions.
- `StopLossSellPips`, `TakeProfitSellPips`, `TrailingStopSellPips` – risk controls for short positions.
- `TrailingStepPips` – minimum favourable move before the trailing stop is tightened.
- `UseTrendFilter` – enables or disables the higher timeframe confirmation.

## Notes

- All pip-based distances are multiplied by the instrument `PriceStep`. Ensure it is configured correctly for accurate risk levels.
- The entry checks are executed on every finished candle; live ticks simply update the most recent bar without changing the decision flow.
- The strategy issues only market orders and expects execution to occur immediately as in the MetaTrader version.
