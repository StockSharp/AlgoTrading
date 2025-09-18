# SR Breakout Strategy

## Summary
SR Breakout Strategy monitors support and resistance levels derived from Donchian Channels on two timeframes (H1 and H4). When a completed candle closes above resistance or below support, the strategy writes an informational log message. The implementation mirrors the alerting logic of the original MQL4 expert without placing any orders.

## How It Works
1. Two candle subscriptions are created: one for the 1-hour timeframe and another for the 4-hour timeframe.
2. Each subscription is bound to its own `DonchianChannels` indicator with a configurable lookback length (default `26`).
3. Once the indicator is formed, the strategy keeps track of the previous candle close for each timeframe.
4. On every finished candle, the current close is compared with the Donchian upper and lower bands:
   - If the close moves from below to above the upper band, a "cross above resistance" message is logged.
   - If the close moves from above to below the lower band, a "cross below support" message is logged.
5. The logic reproduces the notification behavior of the MQL4 script by using `LogInfo` entries as alerts.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `LookbackLength` | Number of candles used to compute Donchian support/resistance. | 26 |
| `Hour1CandleType` | Candle type for the one-hour subscription. | `TimeFrame(1h)` |
| `Hour4CandleType` | Candle type for the four-hour subscription. | `TimeFrame(4h)` |

## Signals
- **H1 breakout** – logs when the one-hour candle close crosses above resistance or below support.
- **H4 breakout** – logs when the four-hour candle close crosses above resistance or below support.

## Notes
- The strategy is intended for alerting only; it does not execute trades.
- Both candle subscriptions must provide high and low data for the Donchian indicator to operate correctly.
- Adjust the lookback length or candle types to match other trading sessions or instruments.
