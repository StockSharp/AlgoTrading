# Spectr Analysis Chaikin Strategy

This strategy uses the Chaikin oscillator to detect shifts in momentum. The oscillator is calculated from the Accumulation/Distribution line smoothed by two linear weighted moving averages. When the slope of the oscillator turns upward and the latest value crosses above the previous value, a long position is opened. Conversely, when the slope turns downward and the latest value crosses below the previous value, a short position is opened.

## Parameters

| Name | Description |
|------|-------------|
| `FastMaPeriod` | Period of the fast linear weighted moving average used in the Chaikin oscillator. |
| `SlowMaPeriod` | Period of the slow linear weighted moving average used in the Chaikin oscillator. |
| `BuyPosOpen` | Enable opening long positions. |
| `SellPosOpen` | Enable opening short positions. |
| `BuyPosClose` | Enable closing long positions when conditions are met. |
| `SellPosClose` | Enable closing short positions when conditions are met. |
| `CandleType` | Timeframe of candles used for calculation. |

## Notes

- Market orders are used for entries and exits.
- The strategy does not set stop-loss or take-profit orders.
- Only the C# version is provided; a Python implementation is not included.
