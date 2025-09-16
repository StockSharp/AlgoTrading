# CashMachine Strategy

## Overview
The CashMachine strategy mirrors the original MQL4 expert advisor that traded a hedged EURUSD/USDCHF basket. It simultaneously opens positions on a base symbol (the strategy `Security`) and on a hedge symbol when three independent filters align:

1. The fast EMA of the base symbol closes above or below the slow EMA.
2. The RSI of the base symbol signals an oversold (< = 30) or overbought (> = 70) condition.
3. The Pearson correlation between the daily deviations of the base and hedge symbols is negative, suggesting that both legs should move together when momentum resumes.

The strategy continuously monitors both instruments, keeps the positions balanced with identical volumes, and closes every open trade once the combined floating profit reaches the configured `TakeProfit` threshold.

## Logic details
1. **Indicator preparation**
   - Subscribes to the intraday `CandleType` for both symbols and builds two EMAs plus one RSI on the base instrument.
   - Subscribes to the higher `DailyCandleType` for both instruments and feeds daily closes into a simple moving average with length `CorrelationLookback`.
   - Computes daily deviations as `close - SMA` and stores the latest `CorrelationLookback` pairs to calculate Pearson correlation.
2. **Trading window**
   - Replicates the original expert's restriction: trading is only allowed during the first five calendar days of each month and stops after 18:00 on the fifth day.
3. **Entry conditions**
   - No open exposure on either leg.
   - Negative correlation (correlation < 0) with at least `CorrelationLookback` samples collected.
   - For longs: `fast EMA > slow EMA` and `RSI <= RsiOversold`. Both base and hedge symbols are bought with identical, normalized volumes.
   - For shorts: `fast EMA < slow EMA` and `RSI >= RsiOverbought`. Both legs are sold.
4. **Profit taking**
   - Every completed candle updates the latest close prices. If the combined floating PnL of both legs exceeds `TakeProfit`, the strategy closes both positions immediately.
5. **Position tracking**
   - Average entry prices and volumes are tracked separately for the base and hedge instruments so that floating PnL can be evaluated from streaming prices.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `TakeProfit` | `decimal` | `10` | Total floating profit (base + hedge) that triggers liquidation of both legs. |
| `EmaShortPeriod` | `int` | `8` | Length of the fast EMA calculated on the base symbol. |
| `EmaLongPeriod` | `int` | `21` | Length of the slow EMA calculated on the base symbol. |
| `RsiPeriod` | `int` | `14` | RSI period for the momentum filter. |
| `RsiOversold` | `decimal` | `30` | RSI threshold that allows long entries. |
| `RsiOverbought` | `decimal` | `70` | RSI threshold that allows short entries. |
| `CorrelationLookback` | `int` | `60` | Number of daily deviation pairs used for the Pearson correlation. |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Intraday candles for EMA/RSI calculations. |
| `DailyCandleType` | `DataType` | `TimeSpan.FromDays(1).TimeFrame()` | Higher timeframe used to build the correlation statistics. |
| `HedgeSecurity` | `Security` | `null` | Second instrument traded alongside the base symbol. Must be provided before starting the strategy. |

> **Volume note:** the StockSharp `Volume` property controls the order size for both legs. The strategy automatically normalizes volumes to each instrument's `VolumeStep`, `VolumeMin`, and `VolumeMax` values and defaults to `0.1` if `Volume` is unset.

## Entry and exit summary
- **Long basket**
  - Conditions: `fast EMA > slow EMA`, `RSI <= RsiOversold`, correlation < 0, no open exposure.
  - Action: buy base symbol at market, buy hedge symbol at market.
- **Short basket**
  - Conditions: `fast EMA < slow EMA`, `RSI >= RsiOverbought`, correlation < 0, no open exposure.
  - Action: sell base symbol at market, sell hedge symbol at market.
- **Exit**
  - Triggered automatically when the combined floating profit reaches `TakeProfit`.
  - Manual exit is also forced whenever new signals are blocked by the time filter (trading simply stops after the window, mirroring the original EA behavior).

## Implementation notes
- The strategy keeps correlation statistics in fixed-size buffers to match the MQL implementation that reuses only the latest `Period()` values.
- The daily deviation is identical to the original `iClose - iMA` computation executed on D1 data.
- No Python port is provided, as requested.
