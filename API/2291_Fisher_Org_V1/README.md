# Fisher Org v1 Strategy

This strategy uses the Fisher Transform indicator to capture trend reversals. A long position is opened when the indicator forms a local minimum, while a short position is opened when a local maximum appears. Opposite signals close any existing position.

## Rules
- **Long**: `Fisher[t-2] > Fisher[t-1]` and `Fisher[t-1] <= Fisher[t]`
- **Short**: `Fisher[t-2] < Fisher[t-1]` and `Fisher[t-1] >= Fisher[t]`

## Parameters
- `Fisher Length` – period of the Fisher Transform (default 7)
- `Candle Type` – timeframe of candles used for calculations

## Indicators
- Fisher Transform
