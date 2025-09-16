# Exp Rj SlidingRangeRj Digit System Tm Plus Strategy

## Overview

This StockSharp strategy is a port of the MetaTrader expert advisor `Exp_Rj_SlidingRangeRj_Digit_System_Tm_Plus`. It recreates the original trading logic based on the custom **Rj_SlidingRangeRj_Digit** channel indicator and preserves the configurable trade management options. The strategy monitors finished candles on a configurable timeframe, detects breakouts beyond the channel, and reacts to those events with delayed entries, optional timed exits, and price-based stop/target management.

## Indicator logic

The Rj_SlidingRangeRj_Digit indicator builds an adaptive price channel using a multi-step averaging process:

1. For the upper band, the highest high within `UpCalcPeriodRange` bars is calculated for each of the last `UpCalcPeriodRange` sliding windows, shifted by `UpCalcPeriodShift` bars. The average of these maxima is rounded to the precision specified by `UpDigit`.
2. The lower band repeats the same logic on lows using `DnCalcPeriodRange`, `DnCalcPeriodShift`, and `DnDigit`.
3. A candle is labelled as a breakout when its close price is above the upper band (colors `2` / `3`) or below the lower band (colors `0` / `1`). Candles inside the channel produce a neutral color (`4`).

The strategy streams finished candles, rebuilds the bands on each update, and stores the most recent color codes to mimic the `CopyBuffer`/`SignalBar` behaviour from the MQL implementation.

## Trading rules

* **Entry delay:** Signals are evaluated on the bar defined by `SignalBar` (default one bar ago). The strategy waits until a breakout color appears and the previous bar did not have the same breakout color. This reproduces the original one-bar delay before taking a trade.
* **Long entries:** Enabled by `EnableBuyEntries`. A bullish breakout (`color 2` or `3`) triggers a market buy when no long position is open (short exposure is netted out automatically).
* **Short entries:** Enabled by `EnableSellEntries`. A bearish breakout (`color 0` or `1`) triggers a market sell when no short position is open.
* **Exit signals:**
  * Longs close on bearish breakout colors if `EnableBuyExits` is true.
  * Shorts close on bullish breakout colors if `EnableSellExits` is true.
  * Optional time-based exit (`UseTimeExit`) closes any open position once it has been held longer than `ExitMinutes`.
  * Optional stop-loss and take-profit levels expressed in points (`StopLossPoints`, `TakeProfitPoints`) are converted into price offsets using the instrument `PriceStep`.

All actions use `BuyMarket` / `SellMarket` so the strategy automatically reverses positions when necessary.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle type (timeframe) used for signal detection. | 8-hour candles |
| `EnableBuyEntries` / `EnableSellEntries` | Allow long/short breakout entries. | `true` |
| `EnableBuyExits` / `EnableSellExits` | Allow indicator-based exits for longs/shorts. | `true` |
| `UseTimeExit` | Close trades after a fixed holding time. | `true` |
| `ExitMinutes` | Holding time limit in minutes. | `1920` |
| `UpCalcPeriodRange`, `UpCalcPeriodShift`, `UpDigit` | Parameters of the upper channel band. | `5`, `0`, `2` |
| `DnCalcPeriodRange`, `DnCalcPeriodShift`, `DnDigit` | Parameters of the lower channel band. | `5`, `0`, `2` |
| `SignalBar` | Bar offset used for evaluating breakout signals. | `1` |
| `StopLossPoints`, `TakeProfitPoints` | Stop-loss / take-profit in price points (converted with `PriceStep`). | `1000`, `2000` |

Set the strategy `Volume` property to control position sizing. The stop-loss and take-profit parameters are optional; set them to `0` to disable either protection level.

## Notes

* The strategy expects sufficient history to form the sliding channel (roughly `max(shift + 2 × range)` candles). It automatically manages the internal buffers and ignores signals until enough data is available.
* Price rounding is performed using decimal digits, mirroring the MQL indicator rounding behaviour.
* Python implementation is intentionally omitted as per the project instructions; only the C# version is provided.
