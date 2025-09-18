# Refresh28ChartsV3Strategy

## Overview
Refresh28ChartsV3Strategy ports the MetaTrader 4 utility *Refresh28Charts v3.mq4* to the StockSharp high level strategy API. The expert advisor opened every symbol/time frame chart used by the basket and forced MetaTrader to cache a configurable number of historical bars. The StockSharp version reproduces this preparation phase by subscribing to the requested instruments and waiting until enough finished candles are received for each combination.

## Original MQL Logic
- Enumerates 28 major Forex pairs together with nine time frames (M1, M5, M15, M30, H1, H4, D1, W1, MN1).
- Opens a chart for each pair and time frame, disables auto scroll and repeatedly navigates backwards until *BarsToRefresh* candles are available.
- Logs a warning when the platform cannot deliver the requested amount of history after multiple attempts.
- Closes every temporary chart once the history cache is populated.

## StockSharp Implementation
- Uses `SubscribeCandles` to request historical and live data for every resolved symbol/time frame pair without creating chart windows.
- Keeps track of each subscription through an internal state object that counts the number of completed candles and logs progress every 20% of the target.
- Logs when the requested bar count is reached for a pair/time frame and announces when the entire batch finishes loading.
- Resolves securities through the connected `SecurityProvider`. The default symbol list mirrors the MQL script but traders can provide custom identifiers via the `Symbols` parameter.
- Approximates the weekly and monthly time frames with `TimeSpan.FromDays(7)` and `TimeSpan.FromDays(30)` respectively because StockSharp expresses candle types as `TimeSpan` instances.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `BarsToRefresh` | Minimum number of finished candles to cache for every symbol and time frame. Configured as an optimizable positive integer. | `50` |
| `Symbols` | Comma separated list of security identifiers to refresh. When empty the strategy falls back to `Strategy.Security`. | `EURGBP,GBPAUD,...,EURJPY` |

## Usage
1. Connect a data source that provides the requested instruments and assign it to the strategy.
2. Leave `Symbols` unchanged to work with the default 28 Forex pairs or replace it with a custom comma/semicolon separated list.
3. Set `BarsToRefresh` to the number of historical candles that should be present before the platform is considered ready.
4. Start the strategy. The log stream will display progress updates and a completion message once every combination meets the bar requirement.

## Notes
- The strategy does not place orders; it only ensures that historical data is cached before other strategies start trading.
- If the data source cannot deliver the required amount of history, the strategy keeps the subscription active and continues reporting progress for any additional candles that arrive.
- Combine the strategy with StockSharp automation scripts or startup sequences to refresh the cache before launching trading systems.
