# Bollinger Band Pending Stops Strategy

## Overview
This sample converts the original MQL "Bb_0_1" expert advisor into the StockSharp high level API. The strategy listens to one candle subscription and uses Bollinger Bands to bracket the current price. When the market sits between the upper and lower bands, the algorithm places three layered buy stop orders above price and three layered sell stop orders below price. Each layer is configured with individual take-profit distances while sharing the same stop reference taken from the opposite band.

## Trading logic
- Subscribe to the configured timeframe and calculate Bollinger Bands with the requested period and deviation.
- Inside the trading window (`StartHour` < hour < `EndHour`) and while the price remains between the bands, place pending orders:
  - Three buy stops at the current upper band level with take-profits displaced by `FirstTakeProfit`, `SecondTakeProfit`, and `ThirdTakeProfit` price steps above the entry.
  - Three sell stops at the current lower band level with mirrored take-profits below the entry.
  - All entries inherit the opposite band as their initial protective stop.
- Pending orders are automatically re-registered whenever the bands move closer to price so that the orders follow the indicator envelopes.
- Once a stop order executes, the strategy registers explicit stop-loss and take-profit orders for the filled volume.
- Trailing protection is optional: `UseBandTrailingStop` selects the opposite band for trailing, otherwise the middle band (EMA) is used. Stops only trail when the close moves beyond the entry price and the indicator value provides a better level.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Time frame used for the Bollinger Band calculations. |
| `BandPeriod` | Number of candles used by the bands. |
| `BandDeviation` | Standard deviation multiplier for the bands. |
| `Volume` | Volume of each pending layer. |
| `StartHour` / `EndHour` | Hourly trading window (exclusive bounds). |
| `FirstTakeProfit`, `SecondTakeProfit`, `ThirdTakeProfit` | Take-profit distances expressed in price steps for every layer. |
| `UseBandTrailingStop` | Select the trailing reference: opposite band (`true`) or Bollinger middle line (`false`). |

## Implementation notes
- Order volume mirrors the original expert advisor by using a static size (`Volume`). Risk-based position sizing from the MQL code is not implemented because the StockSharp sample environment does not provide account history.
- Indicator shift parameters from the MQL script are not exposed because the high level API already delivers aligned values for the current candle.
- Protective orders are normal stop and limit orders that are refreshed whenever the band-based trailing conditions improve the stop level.
