# Chaikin Volatility Stochastic Strategy

This strategy applies a stochastic oscillator to Chaikin volatility values to capture trend reversals. The high-low range of each candle is smoothed with an EMA, then normalized with a stochastic calculation and finally smoothed by a weighted moving average.

When the smoothed oscillator turns downward after rising, a long position is opened and any short position is closed. When the oscillator turns upward after falling, a short position is opened and any long position is closed.

## Parameters
- **Candle Type**: timeframe for candle subscription.
- **EMA Length**: smoothing period for the high-low range.
- **Stochastic Length**: lookback period for the stochastic calculation.
- **WMA Length**: weighted moving average period for smoothing the oscillator.
- **Enable Longs / Enable Shorts**: toggle allowed trade directions.

## Indicators
- ExponentialMovingAverage
- Highest and Lowest
- WeightedMovingAverage

## Trading Rules
- **Long Entry**: oscillator was rising and turns downward.
- **Short Entry**: oscillator was falling and turns upward.
- Opposite positions are closed on signal change.
