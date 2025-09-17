# HPCS Inter5 Strategy

## Overview

The **HPCS Inter5 Strategy** is a single-shot momentum script converted from the MetaTrader 4 expert `_HPCS_Inter5_MT4_EA_V01_WE`. When the strategy starts it inspects the latest completed candles and, if the close price from five bars ago is higher than the most recent close, it submits one market buy order. Optional protective stop-loss and take-profit distances emulate the pip-based behaviour of the original EA.

## Trading Logic

1. Subscribe to the configured candle series and maintain the last six completed closes.
2. After the buffer is filled, compare the close from five bars ago with the latest close (`Close[5] > Close[1]` in MetaTrader terms).
3. If the condition is satisfied and no trade has been placed yet, send a market buy order with the configured volume.
4. Protective orders are armed once at start-up through `StartProtection`, using MetaTrader-style pip conversion: instruments with 3 or 5 decimals multiply `PriceStep` by 10 to determine the pip size, otherwise the raw `PriceStep` is used.

The strategy does not open additional trades and ignores every subsequent signal once the first position is filled.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `Candle Type` | 1 minute time frame | Candle type used to collect the close prices. Set it to the timeframe that matches your desired signal interval. |
| `Stop Loss (pips)` | 10 | Distance for the protective stop-loss in MetaTrader pips. A value of `0` disables the stop. |
| `Take Profit (pips)` | 10 | Distance for the protective take-profit in MetaTrader pips. A value of `0` disables the take profit. |
| `Trade Volume` | 1 | Volume of the market order submitted when the entry condition triggers. |

## Implementation Notes

- The strategy requires a configured `Security.PriceStep` (or `Security.Step`) to convert pip distances. If this information is missing the protective offsets remain inactive but the entry signal still works.
- Only finished candles (`CandleStates.Finished`) are processed to match the MetaTrader behaviour that relies on `Close[1]` and older values.
- The internal buffer holds exactly six closes without using indicator history, respecting the minimalistic nature of the source EA.
- `IsFormedAndOnlineAndAllowTrading()` is checked before sending the order to ensure the environment is ready for execution.

## Usage Tips

1. Assign a Forex instrument with proper price and volume settings.
2. Adjust the `Candle Type` to match the timeframe you want to analyse.
3. Leave the stop-loss or take-profit at zero if you prefer to manage exits manually.
4. Restart the strategy whenever you want to re-evaluate the entry condition because it fires only once per session.
