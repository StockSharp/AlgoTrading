# FORTS Currency Power Strategy

This strategy reproduces the **FORTS Currency Power** data feed from MetaTrader 5 inside StockSharp. It monitors three custom baskets built from Moscow Exchange FORTS futures and reports their relative strength on a 0-100 scale. The baskets are recalculated whenever a new completed candle arrives for the constituent contracts.

## Overview

- Calculates Donchian-based currency power for three baskets: `RTS`, `USD`, and `RUB`.
- Subscribes to the same candle type for every instrument and processes only finished candles.
- Normalizes each contract so that values remain between 0 and 100, mirroring the original script.
- Logs basket values for visualization or further processing by external components.
- Does **not** place any trades; it is an analytical monitor.

## Basket construction

| Basket | Contracts (sign indicates inversion) |
| --- | --- |
| RTS | `MIX` (+), `RTS` (+) |
| USD | `Si` (+), `RTS` (-) |
| RUB | `Si` (-), `MIX` (-), `Eu` (-) |

A positive sign keeps the Donchian normalization, while a negative sign inverts it (100 − value). The final power is the simple average of the adjusted components, exactly like the MQL version.

## Parameters

| Parameter | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle data type shared by every subscribed instrument. | `1 minute` time-frame |
| `Lookback` | Number of recent candles in the Donchian channel range. | `5` |
| `MixSecurity` | FORTS MIX futures contract (e.g., `MIX-3.18`). | Required |
| `RtsSecurity` | FORTS RTS index futures contract (e.g., `RTS-3.18`). | Required |
| `SiSecurity` | USD/RUB futures contract (e.g., `Si-3.18`). | Required |
| `EuSecurity` | EUR/RUB futures contract (e.g., `Eu-3.18`). | Required |

### Logging output

Every time all components provide fresh values, the strategy emits an informational log entry:

```
RTS basket power = 61.45 at 2018-02-05T10:31:00.0000000+03:00
USD basket power = 42.07 at 2018-02-05T10:31:00.0000000+03:00
RUB basket power = 37.82 at 2018-02-05T10:31:00.0000000+03:00
```

These messages can be routed to dashboards, stored, or forwarded to other strategies.

## Usage guidelines

1. Assign the four futures instruments through the strategy parameters before starting.
2. Pick a candle type that matches the original feed (1-minute bars are recommended).
3. Attach the strategy to a portfolio if you plan to extend it with trading rules; otherwise a dummy portfolio is sufficient.
4. Optionally link the log output to a charting widget to draw the power curves or export the values.

## Notes

- Because the Donchian channel needs a full window, the power will stay empty until the specified number of candles has been processed.
- The strategy protects against division by zero whenever the Donchian range collapses.
- Adjusting the `Lookback` parameter changes the responsiveness of the indicator: shorter windows react faster but are noisier.
