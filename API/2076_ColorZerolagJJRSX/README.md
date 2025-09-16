# Color Zerolag JJRSX Strategy

This strategy replicates the logic of the **ColorZerolagJJRSX** MetaTrader expert. It uses two smoothed RSI oscillators to approximate the original ColorZerolagJJRSX indicator. The fast and slow lines cross to generate trading signals.

## How It Works

- When the fast oscillator crosses **below** the slow oscillator, the strategy closes any short position and optionally opens a new long position.
- When the fast oscillator crosses **above** the slow oscillator, the strategy closes any long position and optionally opens a new short position.
- Protective stop-loss and take-profit levels are applied using the built-in `StartProtection` mechanism.

## Parameters

| Name | Description |
| --- | --- |
| `FastPeriod` | Period of the fast JJRSX line. |
| `SlowPeriod` | Period of the slow JJRSX line. |
| `BuyOpen` | Allow opening long positions. |
| `SellOpen` | Allow opening short positions. |
| `BuyClose` | Close existing long positions on opposite signal. |
| `SellClose` | Close existing short positions on opposite signal. |
| `StopLoss` | Stop-loss level in price units. |
| `TakeProfit` | Take-profit level in price units. |
| `CandleType` | Time frame used for calculations. |

## Notes

- The implementation uses built-in indicators and high-level `Bind` API.
- Volume is taken from the strategy's `Volume` property.
- No Python version is provided for this strategy.

## References

Original MQL source located in `MQL/13854` within this repository.

