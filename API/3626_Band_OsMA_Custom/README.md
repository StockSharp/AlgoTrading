# BandOsMaCustom Strategy

## Overview

This strategy is a direct port of the MetaTrader 5 expert advisor located at
`MQL/45596/mql5/Experts/MQL5Book/p7/BandOsMACustom.mq5`. The original robot
combines the MACD histogram (also known as OsMA) with Bollinger Bands and a
moving average that are applied to the histogram values instead of raw prices.
Whenever the histogram pierces the lower band, the expert opens a long trade,
while touches of the upper band trigger short entries. The histogram crossing a
separate moving average closes the position. A protective stop and a
trailing-stop step (equal to one-fiftieth of the stop) keep risk under control.

The StockSharp implementation preserves this behaviour using the high-level API,
so the trading logic stays readable and debuggable inside the framework.

## Conversion highlights

* The MACD histogram is implemented through
  `MovingAverageConvergenceDivergenceHistogram`, fed with the candle price that
  corresponds to the MetaTrader `PRICE_*` mode selected by the `AppliedPrice`
  parameter.
* Bollinger Bands and the exit moving average process the OsMA output rather
  than price data. A compact history buffer reproduces the MetaTrader `shift`
  arguments for both indicators.
* The strategy keeps the original long/short signalling: crossings below the
  lower band start longs, crossings above the upper band start shorts, and the
  OsMA crossing its moving average closes the trade.
* `StartProtection` mirrors the MetaTrader stop-loss plus trailing-stop block.
  The trailing step is calculated as `StopLossPoints / 50`, just like the MQL
  class `TrailingStop` did.

## Indicators

| Indicator | Purpose |
| --- | --- |
| `MovingAverageConvergenceDivergenceHistogram` | Recreates MetaTrader's `iOsMA` output. |
| `BollingerBands` | Calculates upper and lower thresholds over the histogram. |
| Moving average (SMA/EMA/SMMA/LWMA) | Filters exits when the histogram crosses it. |

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 1-hour time-frame | Primary timeframe used for all indicator calculations. |
| `FastOsmaPeriod` | 12 | Fast EMA length from the OsMA calculation. |
| `SlowOsmaPeriod` | 26 | Slow EMA length from the OsMA calculation. |
| `SignalPeriod` | 9 | Signal SMA length from the OsMA calculation. |
| `AppliedPrice` | Typical | MetaTrader-style applied price that feeds the histogram. |
| `BandsPeriod` | 26 | Length of the Bollinger Bands drawn on the histogram values. |
| `BandsShift` | 0 | Right shift (in bars) applied to the Bollinger values. |
| `BandsDeviation` | 2.0 | Standard deviation multiplier for the bands. |
| `MaPeriod` | 10 | Length of the exit moving average calculated on the histogram. |
| `MaShift` | 0 | Right shift (in bars) applied to the exit moving average. |
| `MaMethod` | Simple | Moving-average method (SMA, EMA, SMMA, LWMA). |
| `StopLossPoints` | 1000 | Protective stop distance expressed in price steps. |
| `OrderVolume` | 0.01 | Trading volume, identical to the MetaTrader “Lots” input. |

## Trading rules

1. Subscribe to the selected candle series and feed the chosen applied price
   into the MACD histogram.
2. Pass each histogram value to the Bollinger Bands and the exit moving average.
3. Detect signals using the shifted buffers:
   * If the histogram drops through the lower band, set a bullish signal.
   * If the histogram rises through the upper band, set a bearish signal.
   * When the histogram crosses the exit moving average, clear the active
     signal, which allows the position to be closed.
4. Manage positions:
   * Close existing longs whenever the bullish signal disappears; close shorts
     when the bearish signal disappears.
   * Open a long when the bullish signal is active and there is no open
     position; open a short when the bearish signal is active and the position is
     flat.
5. Apply `StartProtection` with the configured stop-loss distance and a trailing
   step equal to `StopLossPoints / 50` price steps.

## Notes

* All comments in the source code are in English to comply with the repository
  guidelines.
* The history buffers guarantee that the StockSharp version respects MetaTrader
  `BandsShift` and `MaShift` parameters without requesting indicator values by
  index.
* The strategy aligns with the high-level API conventions: `SubscribeCandles`
  drives indicator updates, and direct calls to `BuyMarket`/`SellMarket` mimic
  the original expert's order placement.
