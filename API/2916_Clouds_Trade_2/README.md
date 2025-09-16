# Clouds Trade 2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a C# port of Vladimir Karputov's "cloud's trade 2" expert advisor. It trades breakouts confirmed by two recent Bill Williams fractals and an overbought/oversold crossover on the stochastic oscillator. Trade management mirrors the original inputs with configurable stop loss, take profit, trailing stop, and minimum profit locks.

## Trading Logic

- **Data**: single timeframe candles (default 15 minutes).
- **Indicators**:
  - Stochastic oscillator using the configured %K lookback, slowing, and %D smoothing.
  - Rolling five-candle high/low window to reconstruct upper and lower fractals.
- **Entry**:
  - **Long**: two consecutive lower fractals appear more recently than any upper fractal **or** the stochastic %D drops below 20 while crossing beneath %K. No position must be open and the optional one-trade-per-day filter must allow a new entry.
  - **Short**: two consecutive upper fractals appear first **or** the stochastic %D rises above 80 while crossing above %K.
- **Exits & Protection**:
  - Static stop loss and take profit offsets from the entry price.
  - Optional trailing stop that moves only when the current profit exceeds the configured trailing distance plus step.
  - Close positions once either a money-based profit target or a price-distance target is reached.
  - Stops are emulated by inspecting candle highs/lows, matching the broker-managed behavior in the MQL version.

## Parameters

- **Order Volume**: base order size for entries.
- **Stop/Take Offsets**: absolute price distances; adjust to the instrument's tick value to reproduce the original pip-based inputs.
- **Trailing Stop & Step**: offsets in price units that govern when the stop is moved.
- **Min Profit (Currency / Points)**: close trades once unrealized profit exceeds these thresholds.
- **Use Fractals / Use Stochastic**: independently enable either signal component.
- **One Trade Per Day**: prevent multiple entries during the same trading date.
- **Stochastic Settings**: %K lookback, %K slowing, and %D smoothing lengths.
- **Candle Type**: timeframe for the strategy's candle subscription.

## Notes

- Position profit checks approximate the original commission/swap adjustments by using price movement times position size.
- Trailing logic follows the MQL implementation by requiring profit to exceed the trailing distance plus the step before shifting the stop.
- To mimic the default MQL pip-based inputs on Forex pairs, set the stop/take offsets to the desired pip value multiplied by the instrument's point value (for example, 50 pips ≈ 0.005 for five-digit quotes).
