# Time Zone Pivots Open System Strategy

This strategy is a StockSharp high-level API port of the MetaTrader expert `Exp_TimeZonePivotsOpenSystem`. It reproduces the original logic that anchors a symmetric price channel to the daily opening price at a configurable hour and reacts when completed candles break above or below that band. All orders are sent as market orders and optional stop-loss / take-profit protection is configured through `StartProtection`.

## How it works

1. Subscribes to the configured candle timeframe, records the instrument price step and configures protective stops if distances are greater than zero.
2. Tracks the first candle of every day whose opening time matches `StartHour`. The open price of that candle becomes the anchor for the session and defines the upper and lower bands at `OffsetPoints` price steps above and below the anchor.
3. Computes a five-state signal for each finished candle, mirroring the color-coded buffer of the original custom indicator:
   - `0` / `1`: the candle closed above the upper band (bullish breakout, with the index reflecting candle direction).
   - `2`: the candle ended inside the band (neutral).
   - `3` / `4`: the candle closed below the lower band (bearish breakout).
4. Maintains a sliding history of signals. The candle located `SignalBar` steps back serves as the confirmation bar and the candle immediately before it must be neutral to trigger an entry, recreating the MetaTrader logic that waits for a bar after the breakout.
5. When a bullish confirmation appears the strategy optionally closes short positions and, if flat and allowed, opens a new long position. Bearish confirmations behave symmetrically for short trades.
6. After a new position is opened the strategy postpones further entries in the same direction until the next candle following the confirmation bar begins, preventing duplicate orders in the same session.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle timeframe feeding the breakout calculations. | `H1` |
| `OrderVolume` | Volume used for new positions. | `0.1` |
| `StartHour` | Hour (0-23) whose opening price anchors the daily bands. | `0` |
| `OffsetPoints` | Half-width of the band in price steps (tick units). | `100` |
| `SignalBar` | Number of closed candles between the current bar and the breakout confirmation. Must be ≥ 1 in this port. | `1` |
| `StopLossPoints` | Protective stop distance in price steps. | `1000` |
| `TakeProfitPoints` | Profit target distance in price steps. | `2000` |
| `EnableLongEntry` | Allow opening long positions after bullish signals. | `true` |
| `EnableShortEntry` | Allow opening short positions after bearish signals. | `true` |
| `CloseLongOnBearishBreak` | Close existing long positions on bearish confirmations. | `true` |
| `CloseShortOnBullishBreak` | Close existing short positions on bullish confirmations. | `true` |

## Notes

- The money management block from the MetaTrader version is replaced by the explicit `OrderVolume` parameter typical for StockSharp strategies.
- The stop-loss and take-profit parameters are converted from point distances to absolute price offsets using the current instrument price step.
- The S# implementation keeps only one net position (long, short, or flat) exactly like the MQL original, and will skip new entries while a position is still open.
