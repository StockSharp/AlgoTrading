# Three Red / Three Green Strategy with ATR Filter

Enters long after three consecutive bearish candles if ATR is above its 30-period SMA. Exits after three bullish candles or when maximum trade duration is reached.

## Parameters

- **CandleType**: Type of candles.
- **MaxTradeDuration**: Maximum number of bars to keep an open position.
- **UseGreenExit**: Whether to exit after three green candles.
- **AtrPeriod**: Period for ATR calculation (0 disables the filter).
