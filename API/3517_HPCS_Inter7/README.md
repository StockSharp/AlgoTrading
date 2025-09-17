# Hpcs Inter7 Strategy

## Overview
The Hpcs Inter7 strategy is a Bollinger Bands breakout system converted from the MetaTrader 4 expert advisor `_HPCS_Inter7_MT4_EA_V01_We.mq4`. The algorithm monitors standard Bollinger Bands calculated on the selected candle series. When price crosses outside of the bands it interprets this as a momentum breakout and opens a position in the direction of the breakout. For every new entry the strategy immediately places both stop loss and take profit targets at a fixed distance from the entry price to replicate the original expert advisor behavior.

## Trading Logic
- **Short entry**: When the previous candle closed above the lower band and the latest closed candle finishes below the lower band, the strategy opens a market sell. This recreates the original condition `Close[0] < LowerBand[0] && Close[1] > LowerBand[1]`.
- **Long entry**: When the previous candle closed below the upper band and the latest closed candle finishes above the upper band, the strategy opens a market buy. This replicates `Close[0] > UpperBand[0] && Close[1] < UpperBand[1]` from the MQL implementation.
- **Single trade per candle**: The algorithm remembers the opening time of the candle that generated the last order. A new signal on the same candle is ignored to avoid duplicate trades, mirroring the `gdt_Candle` guard variable from MQL4.
- **Protective orders**: Immediately after a new position is opened the strategy calls `SetStopLoss` and `SetTakeProfit` using the configured distance. Both are placed symmetrically around the entry price so the position always has predefined risk and reward targets.

## Parameters
| Name | Description | Default | Optimizable |
| --- | --- | --- | --- |
| `BollingerLength` | Number of candles used to build the Bollinger Bands. | 20 | Yes |
| `BollingerDeviation` | Standard deviation multiplier for the Bollinger Bands width. | 2 | Yes |
| `CandleType` | Candle series used for calculations (defaults to 1 minute time frame). | 1-minute candles | No |
| `ProtectionDistancePoints` | Stop loss and take profit distance expressed in price steps. | 10 | Yes |

## Additional Notes
- The strategy uses the StockSharp high level API (`SubscribeCandles().Bind(...)`) and does not store custom history arrays.
- `StartProtection()` is activated on start so the platform automatically manages protective orders placed by `SetStopLoss` and `SetTakeProfit`.
- Position size is controlled by the base `Strategy.Volume` property, just like the original expert advisor that traded a fixed volume of one lot.
- The strategy was designed for FX instruments where the original EA was deployed, but it can be used on any security that provides meaningful Bollinger Band signals and a valid `PriceStep` value.
