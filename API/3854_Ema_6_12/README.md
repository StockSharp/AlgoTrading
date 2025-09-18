# EMA 6/12 Crossover Strategy

This strategy replicates the MetaTrader expert advisor that trades the crossover between a fast EMA(6) and a slow EMA(12). It subscribes to hourly candles by default, calculates both moving averages, and waits for a confirmed crossover at the close of a candle before acting.

## Trading Logic

- **Entry:**
  - A bullish signal appears when EMA(6) crosses above EMA(12). The strategy opens a long position if there is no active position.
  - A bearish signal appears when EMA(6) crosses below EMA(12). The strategy opens a short position if there is no active position.
- **Exit:**
  - When `UseCloseSignals` is enabled (default behaviour), the strategy closes the current position once an opposite crossover is detected. It waits for the next crossover before opening a new trade, mirroring the original expert advisor.
  - Optional take profit and trailing stop protections are managed via StockSharp's built-in `StartProtection` helper.
- **Position sizing:**
  - Orders use the `OrderVolume` parameter (default 1 lot). Volumes are aligned to the security settings before sending orders.

## Risk Management

- **Trailing stop:** Converts the original "points" setting into price steps. When greater than zero, the stop automatically trails in the direction of the trade once the position becomes profitable.
- **Take profit:** Expressed in price steps. Set to zero to disable.
- The strategy never averages down or pyramids. Only one position per symbol is allowed.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Time frame used to build candles and EMAs. Defaults to 1 hour. |
| `OrderVolume` | Trade size in lots. |
| `ShortEmaLength` | Period for the fast EMA (default 6). |
| `LongEmaLength` | Period for the slow EMA (default 12). |
| `UseCloseSignals` | Close the current position on an opposite crossover (default: enabled). |
| `TrailingStopSteps` | Trailing distance in price steps. Zero disables trailing. |
| `TakeProfitSteps` | Take profit distance in price steps. Zero disables it. |

## Notes

- Signals are processed only on finished candles to avoid intrabar noise.
- The previous EMA values are reset whenever the position returns to zero, ensuring clean detection for the next crossover.
- All code comments are written in English, and indentation uses tabs in accordance with project guidelines.
