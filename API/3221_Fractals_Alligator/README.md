# Fractals & Alligator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the MetaTrader "Fractals & Alligator" expert by combining Bill Williams Alligator alignment with fractal breakouts, a momentum confirmation layer, and range filters. It processes finished candles on a higher timeframe to emulate the original multi-timeframe logic.

## Details
- **Entry Criteria**: Wait for the Alligator lips, teeth, and jaw to widen in the same direction while a fresh fractal forms beyond the mouth. A long setup requires the close to break the latest bullish fractal above the teeth and any of the last three momentum readings to exceed the buy threshold. Shorts mirror the rules on the downside.
- **Long/Short**: Opens both long and short trades. Only one net position is maintained; new signals reverse the existing exposure.
- **Exit Criteria**: Positions are closed when the opposite fractal is penetrated or when the Alligator alignment collapses. Protective orders handle remaining exits.
- **Stops**: Uses StockSharp protective orders for stop-loss, take-profit, and an optional trailing stop in price steps, matching the original money-management idea.
- **Default Values**: Alligator lengths 13/8/5 with shifts 8/5/3, 14-period momentum, 10-bar range lookback, 20-step fixed box (if ATR filter disabled), take-profit 50 steps, stop-loss 20 steps, trailing stop 40 steps.
- **Filters**: Optional ATR multiplier confirms that price has moved at least one ATR away from the recent range; otherwise a fixed box expressed in price steps is used. Momentum thresholds (0.3%) suppress low-energy breakouts.
