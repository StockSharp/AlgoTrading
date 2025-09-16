# Chaos Trader Lite Strategy

The Chaos Trader Lite strategy replicates Bill Williams' three wise men entry techniques using StockSharp's high level API. It analyses each finished candle of the configured timeframe (1 hour by default) and places stop orders when any of the following conditions are met:

1. **First Wise Man – Divergent bar**: detects bullish or bearish divergent candles and requires a minimum gap between price and the Alligator lips line.
2. **Second Wise Man – Awesome Oscillator acceleration**: waits for five consecutive Awesome Oscillator readings to show accelerating momentum.
3. **Third Wise Man – Fractal breakout**: confirms a fractal two candles back and checks that price is trading sufficiently far from the Alligator teeth line before queuing a breakout order.

Whenever a long setup appears the strategy cancels existing sell stops, closes short positions, places a new buy stop just above the previous high and records a protective stop below the candle. The opposite happens for short setups. Protective stops are monitored on each bar; if price crosses the stored level the position is exited at market.

## Indicators and calculations

- **Alligator lips**: 5-period smoothed moving average of the median price shifted three candles forward. The strategy keeps a queue so that the value aligned with the current bar matches the MetaTrader implementation.
- **Alligator teeth**: 8-period smoothed moving average of the median price shifted five candles forward. The shifted value drives the third wise man filter.
- **Awesome Oscillator**: StockSharp's built-in indicator (5 vs 34 SMA of the median price) provides the momentum series used by the second wise man.
- **Fractals**: the code inspects the high/low of the candle that sits two bars behind the latest bar. A valid fractal requires that candle to be higher (or lower) than the two candles on both sides.

## Trading logic

1. Subscribe to the requested candle type and process only finished candles.
2. Update Alligator and Awesome Oscillator indicators and store shifted values.
3. Evaluate the wise men conditions:
   - Divergent bar must close in the upper (for bulls) or lower (for bears) half of the candle and show a distance from the lips greater than `MagnitudePips * PriceStep`.
   - AO acceleration requires five values: `AO[1] > AO[2] > AO[3] > AO[4]` and `AO[4] < AO[5]` for longs, mirrored for shorts.
   - Fractal breakout checks that price closes above (or below) the confirmed fractal and above (or below) the Alligator teeth plus the magnitude threshold.
4. When a setup is active place a `BuyStop` or `SellStop` order with volume `Volume` at the candle high plus one price step (or low minus one step). Cancel the opposite stop and flatten contrary positions.
5. Update stored stop-loss levels: long stops trail upwards, short stops trail downwards. If a candle pierces the stored stop the strategy exits the open position at market.

## Parameters

- `MagnitudePips` *(default 10)* – minimal pip distance between the divergent bar and the Alligator lips.
- `UseFirstWiseMan` *(default true)* – enable or disable the divergent bar entry.
- `UseSecondWiseMan` *(default true)* – enable or disable the Awesome Oscillator acceleration entry.
- `UseThirdWiseMan` *(default true)* – enable or disable the fractal breakout entry.
- `Volume` *(default 0.01)* – order size for stop entries.
- `CandleType` *(default 1 hour)* – data type processed by the strategy.

## Notes

- Bid/ask checks from the original MQL4 code are approximated with the candle close price in StockSharp.
- Margin and volume validation routines from MetaTrader are omitted because StockSharp handles order validation internally.
- Stop orders are cancelled when the opposite setup appears to avoid conflicting pending orders, matching the `CloseAll` behaviour from the EA.
