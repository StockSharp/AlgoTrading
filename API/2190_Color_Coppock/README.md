# Color Coppock Strategy

The **Color Coppock Strategy** implements a trading system based on a modified Coppock oscillator. The oscillator sums two Rate of Change (ROC) values and smooths the result with a moving average. Rising momentum generates long signals, while falling momentum generates short signals.

## How It Works

1. Calculate two ROC values with different periods.
2. Sum both ROC values and apply a Simple Moving Average for smoothing.
3. Compare the current oscillator value with the previous two values:
   - If the oscillator turns upward after declining, the strategy enters a long position or closes an existing short.
   - If the oscillator turns downward after rising, the strategy enters a short position or closes an existing long.
4. Position volume is taken from the strategy's `Volume` property.

## Parameters

| Name | Description |
|------|-------------|
| `Roc1Period` | Period for the first ROC calculation. |
| `Roc2Period` | Period for the second ROC calculation. |
| `SmoothingPeriod` | SMA period applied to the sum of both ROC values. |
| `CandleType` | Candle type used for indicator calculations. |

## Usage

1. Attach the strategy to a security and set the desired parameters.
2. The strategy subscribes to the specified candles and processes only finished candles.
3. Trades are executed with market orders using the default volume.

## Notes

- The strategy uses only high-level API calls such as `SubscribeCandles` and market order helpers.
- All comments inside the code are written in English.
