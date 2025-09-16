# Slope RSI MTF Strategy

## Overview
The **Slope RSI MTF Strategy** ports the MetaTrader 4 expert advisor `SLOPE_RSI_MTF_LBranjord.mq4` together with its companion indicator `Slope_Direction_Line_Alert.mq4`. The original setup stacked multiple Hull moving averages (named "Slope Direction Line") across several timeframes and only opened trades when all of them pointed in the same direction while a four-tier RSI filter confirmed the momentum. The StockSharp version reproduces this multi-timeframe confirmation logic with high-level subscriptions, keeps the ATR-based exit targets, and adds extensive configuration support through strategy parameters.

## Trading logic
1. Subscribe to four candle series for the same instrument: the trading timeframe (`BaseTimeframe`), an hourly confirmation series, a four-hour series, and a daily series.
2. Feed each series into its own `HullMovingAverage` (the StockSharp replacement for the Slope Direction Line) and `RelativeStrengthIndex` instance. The base series uses `SlopeTriggerLength` (default 60) while the confirmation series use `SlopeTrendLength` (default 200).
3. Track the last two Hull values per timeframe. A timeframe is considered bullish when the current Hull value is strictly above the previous one; it is bearish when the Hull value is strictly below the previous value.
4. Simultaneously monitor the RSI on every timeframe:
   - Long setup: RSI must be above `RsiMiddleLevel` (50 by default) but below `RsiUpperBound` (90) on all four series.
   - Short setup: RSI must be below `RsiMiddleLevel` but above `RsiLowerBound` (10) on all four series.
5. When the base timeframe closes and all confirmations are bullish, trigger a long signal. If all confirmations are bearish, trigger a short signal. Signals are ignored until every indicator has produced at least one historical value.
6. Before adding a new position, compute protective distances from ATR values:
   - The hourly series provides the stop-loss distance.
   - The daily series provides the take-profit distance.
7. Market entries add exposure in the signal direction while respecting `MaxOrders`. In the netting environment, opposite exposure is flattened before a new trade is added.
8. Protective levels are recalculated on every scale-in and are evaluated on subsequent base timeframe candles. If the candle’s high/low crosses the stored stop-loss or take-profit level, the strategy exits the full position with a market order.

## Risk management and position sizing
- `UseCompounding` enables the compounding rule from the MQL expert: `volume = PortfolioValue / BalanceDivider`. When disabled, `BaseVolume` is used instead.
- The helper `AdjustVolume` rounds the requested volume to the security’s `VolumeStep` and enforces `MinVolume`/`MaxVolume`. The adjusted value is also written to `Strategy.Volume` so manual actions follow the same size.
- The ATR period (`AtrPeriod`, default 21) mirrors the original settings for both stop-loss and take-profit calculations. The stop uses the hourly ATR while the profit target uses the daily ATR.
- Position counters (`_longEntries`, `_shortEntries`) make sure no more than `MaxOrders` scale-ins are active in any direction at a time.

## Multi-timeframe data handling
- All subscriptions are created with `SubscribeCandles(...)` and processed through `Bind`. The strategy does not cache historical candles manually; indicators react to streaming data and expose their final values through the `Bind` callbacks.
- The `TimeframeState` helper stores Hull and RSI values alongside the previous Hull reading, enabling slope comparisons without requesting historical indicator buffers.
- ATR values are taken only when the corresponding indicator reports `IsFormed`, guaranteeing that stops and targets are calculated from complete bars.

## Parameters
| Name | Type | Default | MetaTrader counterpart | Description |
| --- | --- | --- | --- | --- |
| `SlopeTriggerLength` | `int` | `60` | `SDL1_trigger` | Hull length on the trading timeframe. |
| `SlopeTrendLength` | `int` | `200` | `SDL1_period` | Hull length on hourly, four-hour and daily confirmations. |
| `RsiPeriod` | `int` | `14` | RSI period | RSI lookback applied to every timeframe. |
| `RsiLowerBound` | `decimal` | `10` | RSI lower bound | Lower RSI filter for short signals. |
| `RsiMiddleLevel` | `decimal` | `50` | RSI mid level (implicit) | Neutral RSI level separating long and short regimes. |
| `RsiUpperBound` | `decimal` | `90` | RSI upper bound | Upper RSI filter for long signals. |
| `AtrPeriod` | `int` | `21` | `ATR_Period` | ATR length for stop and take-profit calculations. |
| `MaxOrders` | `int` | `5` | `MaxOrders` | Maximum number of scale-in entries per direction. |
| `UseCompounding` | `bool` | `true` | `compounding` | Enables portfolio-based position sizing. |
| `BaseVolume` | `decimal` | `0.1` | `Lots` | Fixed lot when compounding is disabled. |
| `BalanceDivider` | `decimal` | `100000` | implicit (`AccountBalance()/100000`) | Divider for the compounding formula. |
| `BaseTimeframe` | `DataType` | `5m` | chart timeframe | Candle series that drives trade execution. |
| `HourTimeframe` | `DataType` | `1h` | `PERIOD_H1` | First confirmation series. |
| `FourHourTimeframe` | `DataType` | `4h` | `PERIOD_H4` | Second confirmation series. |
| `DayTimeframe` | `DataType` | `1d` | `PERIOD_D1` | Highest confirmation series. |

## Differences from the original expert advisor
- StockSharp operates in a netting mode, so opposite positions are closed before a new trade is opened. MetaTrader 4 allowed hedging multiple tickets in both directions.
- Protective stops and targets are executed through candle-based monitoring instead of broker-side order modifications. This keeps the logic inside the strategy while reproducing the ATR distances of the original EA.
- Indicator values are supplied by StockSharp’s built-in `HullMovingAverage`, `RelativeStrengthIndex`, and `AverageTrueRange`. No custom indicator buffers are accessed directly, complying with high-level API best practices.
- Parameter metadata, localization-friendly names, and range hints are exposed through `Param(...).SetDisplay(...)`, making the strategy easier to configure and optimize.

## Usage notes
- Keep the confirmation timeframes strictly greater than or equal to the trading timeframe. Mixing shorter periods may produce conflicting signals and defeats the purpose of the multi-timeframe slope confirmation.
- Ensure the security metadata (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) is populated so stop/target rounding and volume adjustments behave correctly.
- Because stop-loss and take-profit monitoring happens once per completed base candle, intrabar exits will occur on the next bar close. If tighter intrabar management is required, reduce the trading timeframe or extend the strategy with tick-level monitoring.
- The Hull slope test requires consecutive values to differ. Flat Hull sequences (equal values) block new trades even if the RSI filters pass, mirroring the "SDL > SDL[1]" condition from the MetaTrader script.
