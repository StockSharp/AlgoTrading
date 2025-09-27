# Pinbar Reversal Strategy

Converted from the original MQL expert advisor `PINBAR.mq4` (folder `MQL/22269`). The strategy detects pin bar reversals on the primary timeframe and confirms them with higher timeframe momentum and MACD filters. It reproduces the spirit of the source system while using StockSharp high-level API features.

## Trading Logic

- **Primary timeframe** – configurable candle type used to identify price action patterns.
- **Higher timeframe** – configurable candle type used to confirm momentum and MACD trend bias.
- **Pin bar detection** – a bar is accepted when the real body is small relative to the full range and one wick dominates the candle (configurable body and wick ratios).
- **Trend filter** – fast EMA must be above (or below) the slow EMA for long (or short) setups, mirroring the LWMA filters from the original code.
- **Momentum confirmation** – momentum on the higher timeframe must be above (long) or below (short) a configurable threshold for at least one of the last three higher-timeframe bars.
- **MACD confirmation** – the MACD value must be above its signal line for long trades and below the signal line for shorts, matching the monthly MACD confirmation used in the MQL expert.
- **Fractal confirmation** – the strategy maintains a rolling five-bar window and requires the presence of the latest bullish/bearish fractal before accepting a new trade, similar to the `FindFractals()` gate in the source.
- **Risk management** – configurable stop-loss, take-profit, break-even trigger/offset and trailing stop logic track the open position. The trade is exited when any level is touched or when the trailing level is breached.

## Entry Rules

### Long Setup
1. Latest candle on the primary timeframe forms a bullish pin bar (long lower wick, small body).
2. Fast EMA > slow EMA.
3. Latest higher timeframe momentum (or one of the two previous values) is above the threshold.
4. Higher timeframe MACD is above its signal line.
5. A bullish fractal has been detected recently and price has not invalidated it.
6. Strategy is flat or short (shorts are reversed).

### Short Setup
1. Latest candle on the primary timeframe forms a bearish pin bar (long upper wick, small body).
2. Fast EMA < slow EMA.
3. Latest higher timeframe momentum (or one of the two previous values) is below the negative threshold.
4. Higher timeframe MACD is below its signal line.
5. A bearish fractal has been detected recently and price has not invalidated it.
6. Strategy is flat or long (longs are reversed).

## Exit Rules

- Stop-loss and take-profit are expressed in percent relative to the entry price.
- Break-even activates once price moves by the trigger percentage; the stop is moved to entry plus/minus an offset.
- Trailing stop activates after the activation percentage is achieved and follows price at the configured distance.
- Opposite signals also reverse the position.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 15-minute candles | Primary timeframe for pattern detection. |
| `TrendCandleType` | 1-hour candles | Higher timeframe for momentum/MACD filters. |
| `FastMaLength` | 6 | Fast EMA length (replaces fast LWMA). |
| `SlowMaLength` | 85 | Slow EMA length (replaces slow LWMA). |
| `MomentumLength` | 14 | Momentum indicator length on higher timeframe. |
| `MomentumThreshold` | 0.1 | Minimum absolute momentum value for confirmation. |
| `MacdFastLength` | 12 | MACD fast EMA length. |
| `MacdSlowLength` | 26 | MACD slow EMA length. |
| `MacdSignalLength` | 9 | MACD signal EMA length. |
| `BodyToRangeRatio` | 0.3 | Maximum body size relative to candle range. |
| `WickRatio` | 0.6 | Minimum dominant wick ratio defining a pin bar. |
| `StopLossPercent` | 2 | Protective stop size in percent. |
| `TakeProfitPercent` | 4 | Profit target size in percent. |
| `BreakEvenTriggerPercent` | 1.5 | Profit required to move the stop to break-even. |
| `BreakEvenOffsetPercent` | 0.2 | Additional offset added to the break-even stop. |
| `TrailingActivationPercent` | 2.5 | Profit threshold for enabling trailing stop. |
| `TrailingDistancePercent` | 1 | Distance of the trailing stop once activated. |

## Notes

- Volume is fixed to 1 contract by default; adjust the strategy volume for different position sizing.
- The fractal detection resets when price breaches the recorded fractal level, requiring a fresh pattern before a new trade.
- Optimisation ranges are included for key parameters to facilitate backtesting and tuning in StockSharp Designer.
