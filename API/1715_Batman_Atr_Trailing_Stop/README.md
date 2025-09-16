# Batman ATR Trailing Stop Strategy

This strategy implements an ATR-based trailing stop approach inspired by the original "Batman" Expert Advisor.
It tracks dynamic support and resistance levels derived from the **Average True Range (ATR)** indicator and reacts when the price crosses these levels.

## Logic

1. Calculate ATR with the configurable period.
2. Determine support and resistance:
   - `support = price - ATR * factor`
   - `resistance = price + ATR * factor`
3. Maintain the closest support or resistance depending on the current trend.
4. When price breaks above the resistance, open a **long** position.
5. When price breaks below the support, open a **short** position.

The price can be either the close price or the typical price `(high + low + close) / 3`.

## Parameters

| Name | Description |
|------|-------------|
| `ATR Period` | Period of the ATR indicator. |
| `ATR Factor` | Multiplier applied to the ATR value to build the stop lines. |
| `Use Typical Price` | If enabled, uses `(High + Low + Close)/3` instead of the close price. |
| `Candle Type` | Type of candles used for calculations. |

## Notes

- The strategy uses the high-level API with `SubscribeCandles` and `Bind`.
- `StartProtection()` is called on start to ensure position safety.
- Trading is performed only on finished candles.
