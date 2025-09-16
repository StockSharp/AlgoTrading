# Ride Alligator Strategy

This strategy implements Bill Williams' Alligator indicator. The lips, teeth and jaw lines are calculated from the median price using smoothed moving averages with lengths derived from a base period via the golden ratio. A long position is opened when the lips cross above the jaws while the teeth remain below. A short position is opened when the lips cross below the jaws while the teeth remain above. For an open position a trailing stop follows the jaw line.

## Parameters
- **Base Period** – root period used to derive Alligator lengths.
- **Candle Type** – timeframe of input candles.

## Indicators
- Smoothed Moving Average (lips, teeth, jaw)

## Entry Rules
- Long when lips cross above jaws and teeth are below.
- Short when lips cross below jaws and teeth are above.

## Exit Rules
- Opposite crossover closes the position.
- Trailing stop at the jaw line exits when price crosses it.
