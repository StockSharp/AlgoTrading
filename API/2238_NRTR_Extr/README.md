# NRTR Extr Strategy

This strategy implements the **Nick Rypock Trailing Reverse** (NRTR) algorithm with additional signal arrows. It is a conversion of the original MQL5 example "Exp_NRTR_extr" to the StockSharp high level API.

## How It Works

- The custom `NrtrExtrIndicator` computes an average range over a configurable period and draws a trailing level that follows price.
- When the price reverses beyond this level the indicator flips direction and emits a buy or sell signal.
- The strategy opens a long position on a buy signal and a short position on a sell signal.
- Existing positions are closed on the opposite signal or when the defined stop loss or take profit levels are hit.

## Parameters

| Name | Description |
| --- | --- |
| `Period` | Number of candles used for average range calculation. |
| `Digits Shift` | Additional precision adjustment applied to the range factor. |
| `Stop Loss` | Protective stop in price points. |
| `Take Profit` | Profit target in price points. |
| `Enable Buy Open` / `Enable Sell Open` | Allow opening long or short positions. |
| `Enable Buy Close` / `Enable Sell Close` | Allow closing existing positions on opposite signals. |
| `Candle Type` | Timeframe of candles used for the indicator. |

## Notes

The indicator is based on the Average True Range to estimate market volatility. For visualization the strategy automatically draws candles and executed trades on the chart area.

