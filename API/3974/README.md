# Natuseko Protrader 4H Strategy

## Overview
The Natuseko Protrader 4H strategy is a StockSharp port of the MetaTrader 4 expert advisor *NatusekoProtrader4HStrategy*. The original
robot combines exponential moving averages, a MACD oscillator filtered by Bollinger Bands, RSI thresholds and the Parabolic SAR to
identify strong breakout candles on the four-hour timeframe. When a qualifying candle appears the system either opens immediately or
waits for a pullback towards the fast EMA before entering. Once positioned the strategy performs partial profit taking and full exits
based on RSI and Parabolic SAR signals, replicating the money-management block present in the MQL code.

## Trading logic
1. Subscribe to the primary candle stream defined by `CandleType` (4-hour candles by default) and process only finished candles.
2. Calculate three exponential moving averages (fast, slow and trend) on closing prices. All three have configurable lengths.
3. Feed the MACD indicator (fast, slow and signal periods taken from the EA) and apply a simple moving average plus Bollinger Bands to
   the MACD main line. The Bollinger mid line acts as the reference level used by the MQL version.
4. Compute the RSI on closing prices and the Parabolic SAR using full candle data. These indicators drive both entries and exits.
5. Detect bullish setup candles when all of the following conditions hold:
   - Fast EMA is above both the slow and trend EMA.
   - RSI is above `RsiEntryLevel` but below `RsiTakeProfitLong`.
   - MACD main line is above both its short SMA and the Bollinger mid line; the SMA is also above the mid line.
   - The candle body is larger than both shadows, meaning the candle closes strongly in the direction of the move.
   - Parabolic SAR sits below the candle close.
6. Detect bearish setups using the symmetrical checks (fast EMA below, RSI between `RsiTakeProfitShort` and `RsiEntryLevel`, MACD values
   below the Bollinger mid line, bearish candle body and SAR above the close).
7. If the qualifying candle is too far from the trend EMA (distance above `DistanceThresholdPoints`), set a pending flag and wait for a
   pullback. A long entry is triggered once price touches the fast EMA while RSI and SAR remain aligned with the bullish scenario; the
   short entry works analogously on pullbacks to the fast EMA from below.
8. When no pullback is required the strategy closes any opposite exposure and opens a new position with `TradeVolume` lots. Stop-loss
   placement follows the EA rules: first preference is given to the Parabolic SAR if `UseSarStopLoss` is enabled, otherwise the trend
   EMA is used. `StopOffsetPoints` is converted to price distance with the instrument price step and applied to the stop level.
9. While a long position is open the strategy continuously recalculates the stop price and manages exits:
   - If price drops below the stop the entire position is closed.
   - After reaching at least `MinimumProfitPoints` of profit (in instrument points) the strategy can close half of the position when the
     RSI exceeds `RsiTakeProfitLong` or when the Parabolic SAR flips above price (controlled by `UseRsiTakeProfit` and
     `UseSarTakeProfit`).
   - Once profit is adequate and RSI falls back below `RsiEntryLevel` the remaining long exposure is closed.
10. Short positions mirror the same rules with the RSI thresholds reversed and SAR checks flipped relative to price.

## Position management
- Partial exits happen at most once per trade side. After closing half of the position the strategy waits for the full-exit condition
  (RSI crossing back through the neutral level or a stop-loss hit).
- Stop-loss prices are recalculated every candle using the latest Parabolic SAR or trend EMA value to stay aligned with the MQL logic.
- When position size returns to zero the internal state (pending-entry flags, stop references and partial-exit markers) is reset so the
  next trade starts cleanly.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 4-hour timeframe | Primary timeframe processed by the strategy. |
| `TradeVolume` | `decimal` | `0.1` | Order volume used for entries. |
| `FastEmaPeriod` | `int` | `13` | Length of the fast EMA filter. |
| `SlowEmaPeriod` | `int` | `21` | Length of the slower EMA filter. |
| `TrendEmaPeriod` | `int` | `55` | EMA used for distance checks and stop-loss placement. |
| `MacdFastPeriod` | `int` | `5` | Fast EMA length inside the MACD indicator. |
| `MacdSlowPeriod` | `int` | `200` | Slow EMA length inside the MACD indicator. |
| `MacdSignalPeriod` | `int` | `1` | Signal moving-average length inside the MACD indicator. |
| `BollingerPeriod` | `int` | `20` | Number of MACD samples used to compute Bollinger Bands. |
| `BollingerWidth` | `decimal` | `1` | Standard-deviation multiplier for the MACD Bollinger Bands. |
| `MacdSmaPeriod` | `int` | `3` | Length of the MACD smoothing SMA. |
| `RsiPeriod` | `int` | `21` | Length of the RSI indicator. |
| `RsiEntryLevel` | `decimal` | `50` | Neutral RSI threshold shared by entry and exit rules. |
| `RsiTakeProfitLong` | `decimal` | `65` | RSI level that enables partial profit taking for long positions. |
| `RsiTakeProfitShort` | `decimal` | `35` | RSI level that enables partial profit taking for short positions. |
| `DistanceThresholdPoints` | `decimal` | `100` | Maximum distance in instrument points between price and the trend EMA before the entry is delayed. |
| `SarStep` | `decimal` | `0.02` | Acceleration step for the Parabolic SAR. |
| `SarMaximum` | `decimal` | `0.2` | Maximum acceleration for the Parabolic SAR. |
| `UseSarStopLoss` | `bool` | `false` | Use the Parabolic SAR to derive the protective stop. |
| `UseTrendStopLoss` | `bool` | `true` | Use the trend EMA to derive the protective stop. |
| `StopOffsetPoints` | `int` | `0` | Additional offset (in points) added to the protective stop price. |
| `UseSarTakeProfit` | `bool` | `true` | Enable partial exits when price crosses the Parabolic SAR. |
| `UseRsiTakeProfit` | `bool` | `true` | Enable partial exits when RSI reaches the take-profit threshold. |
| `MinimumProfitPoints` | `decimal` | `5` | Minimum profit (in points) before partial or full profit-taking rules activate. |

## Differences from the original EA
- StockSharp trades net positions. To emulate MetaTrader’s single-ticket behaviour the strategy automatically closes the opposite
  exposure before opening a new trade in the other direction.
- Money-management helpers are implemented with market orders instead of modifying individual orders because StockSharp does not manage
  per-ticket stops. The effect matches the EA: one partial exit followed by a final exit when RSI momentum fades.
- Price distance calculations rely on the instrument `PriceStep`. If the security does not define a price step the strategy assumes a
  step of 1. Adjust `DistanceThresholdPoints` and `MinimumProfitPoints` accordingly for instruments that use different point sizes.

## Usage tips
- Configure `TradeVolume` according to the instrument’s lot step; the constructor also assigns the same value to `Strategy.Volume` so
  helper methods use the expected size.
- If trades are delayed too often because candles close far from the trend EMA, lower `DistanceThresholdPoints` or disable the filter by
  setting it to zero.
- Charting the strategy is recommended: the code draws candles, the three EMAs, RSI, Parabolic SAR and MACD Bollinger Bands so you can
  visually confirm the converted logic.
- The MACD parameters mirror the EA’s unusual combination (fast=5, slow=200, signal=1). Consider optimising them before going live
  because such a wide slow period produces very smooth but lagging values.
