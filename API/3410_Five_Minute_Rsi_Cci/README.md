# FiveMinuteRsiCci Strategy

`FiveMinuteRsiCciStrategy` is a StockSharp port of the MetaTrader 4 expert advisor **5Mins Rsi Cci EA.mq4**. The original script trades five-minute candles by combining an RSI threshold cross with a smoothed/EMA moving-average filter and the polarity of two CCI indicators. The C# version keeps the same decision logic while using StockSharp's high-level API for data subscriptions, indicator binding, and risk management.

## Trading logic

1. Subscribe to the configured candle type (five-minute timeframe by default) and update five indicators in real time: RSI, a smoothed MA of the open price, an EMA of the open price, plus fast and slow CCIs calculated from typical prices.
2. Each finished candle is evaluated only when no position is open and the current bid/ask spread is below `MaxSpreadPoints` (converted to price units).
3. A long signal requires:
   - the smoothed MA above the EMA,
   - the RSI crossing upward through `BullishRsiLevel` between the previous and current candle,
   - both CCI values above zero.
4. A short signal requires the inverse conditions (smoothed MA below EMA, RSI crossing downward through `BearishRsiLevel`, both CCIs below zero).
5. Order volume reproduces the EA's dynamic position sizing: `LotCoefficient × sqrt(Equity / EquityDivisor)` rounded to the instrument's volume step and constrained by `VolumeMin`/`VolumeMax`.
6. Protective logic is handled by `StartProtection`, which attaches stop-loss, take-profit, and trailing-stop distances converted from MetaTrader points to absolute price offsets.

## Parameters

| Parameter | Default | Description |
| --- | --- | --- |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Timeframe used for indicator updates and signal evaluation. |
| `RsiPeriod` | `14` | Number of candles used in the RSI calculation. |
| `FastSmmaPeriod` | `2` | Period of the fast smoothed moving average applied to open prices. |
| `SlowEmaPeriod` | `6` | Period of the slow EMA applied to open prices. |
| `FastCciPeriod` | `34` | Period of the fast CCI computed from the typical price `(H+L+C)/3`. |
| `SlowCciPeriod` | `175` | Period of the slow CCI computed from the typical price. |
| `BullishRsiLevel` | `55` | RSI threshold that must be crossed upward to arm a long entry. |
| `BearishRsiLevel` | `45` | RSI threshold that must be crossed downward to arm a short entry. |
| `StopLossPoints` | `60` | Stop-loss distance in MetaTrader points (converted to absolute price). Set to `0` to disable. |
| `TakeProfitPoints` | `0` | Take-profit distance in MetaTrader points. Zero keeps the original EA behaviour (no TP). |
| `TrailingStopPoints` | `20` | Trailing-stop distance in MetaTrader points. Zero disables trailing. |
| `LotCoefficient` | `0.01` | Base coefficient used in the dynamic position sizing formula. |
| `EquityDivisor` | `10` | Divisor inside the square root for equity-based sizing (`sqrt(Equity / EquityDivisor)`). |
| `MaxSpreadPoints` | `18` | Maximum allowed spread (in MetaTrader points). Orders are skipped until the spread narrows. |

## Notes

- The spread filter relies on level-1 data; if best bid/ask quotes are unavailable the strategy waits before opening new positions.
- Point-to-price conversion automatically scales by `PriceStep` and the instrument precision (5/3 decimal instruments multiply the step by 10) to mirror MetaTrader's `Point` value.
- Stops and trailing are managed through StockSharp's built-in protection engine with market exits, matching the EA's use of market orders for trailing-stop updates.
