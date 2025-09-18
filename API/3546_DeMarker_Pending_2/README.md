# DeMarker Pending 2 Strategy

## Overview

The strategy replicates the core logic of the MetaTrader expert "DeMarker Pending 2" using the StockSharp high-level API. It evaluates a DeMarker oscillator on the working timeframe and prepares pending buy or sell entries when the indicator crosses configurable thresholds. Orders can be created as stop or limit requests with an additional indent from the current market price. A session filter, spread guard and distance checks keep new entries under control.

## Trading Logic

1. Subscribe to the configured candle series and compute the DeMarker indicator with the selected period.
2. When the previous value is above the lower level and the current value crosses below it, queue a long pending order. When the previous value is below the upper level and the current value crosses above it, queue a short pending order. Only one signal per candle is processed.
3. Pending orders are placed as stop or limit orders using the indent distance expressed in points. Existing orders can be cancelled before the new request if the replacement option is enabled. The strategy limits the total number of open positions plus pending orders and enforces a minimum distance from the current average position price.
4. Long and short positions use optional stop-loss, take-profit and trailing logic. Protective levels are calculated in price points and monitored on every closed candle. Trailing stops adjust once the position earns the activation profit and additional trailing step distance.
5. A spread filter prevents new orders if the best bid/ask spread exceeds the configured threshold. Optional session boundaries can disable new entries outside the allowed trading window.

## Parameters

| Name | Description |
| --- | --- |
| Working Candles | Timeframe used for signals and protective checks. |
| Order Volume | Default volume for pending orders. |
| Stop Loss (pts) | Initial stop-loss distance in price points. |
| Take Profit (pts) | Initial take-profit distance in price points. |
| Trailing Activate (pts) | Profit needed before the trailing stop engages. |
| Trailing Stop (pts) | Distance between price and trailing stop. |
| Trailing Step (pts) | Additional gain required to move the trailing stop. |
| Trail On Close | Update the trailing stop only on finished candles when enabled. |
| Max Positions | Maximum number of open positions plus pending orders. Zero disables the cap. |
| Min Distance (pts) | Minimum distance from the current position price to new pending entries. |
| Use Stop Orders | Place stop orders when true, otherwise limit orders are used. |
| Single Pending | Allow only one active pending order at a time. |
| Replace Pendings | Cancel outstanding pending orders before placing a new one. |
| Pending Offset (pts) | Indent for new pending prices relative to the market. |
| Max Spread (pts) | Maximum allowed spread before skipping order placement. |
| Use Session Filter | Enable or disable the trading window filter. |
| Start Hour/Minute, End Hour/Minute | Session boundaries when the session filter is active. |
| DeMarker Period | Averaging period for the DeMarker oscillator. |
| Upper Level | Threshold that triggers short setups. |
| Lower Level | Threshold that triggers long setups. |

## Notes

* Order expiration and money-management risk sizing from the original expert are not ported. A fixed volume parameter is used instead.
* Stop-loss and take-profit levels are evaluated on closed candles using high/low prices, which may differ from intrabar execution in MetaTrader.
* Trailing logic is evaluated on closed candles only. Real-time tick-based trailing is not reproduced.
* Pending orders rely on the best bid/ask quotes provided by the data source. Ensure level1 subscriptions are available.
