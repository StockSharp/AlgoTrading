# SilverTrend CrazyChart Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the MetaTrader expert "Exp_SilverTrend_CrazyChart" using the StockSharp high-level API. It trades both s
ides of the market by comparing two buffers of the custom SilverTrend CrazyChart indicator. When the delayed band crosses the cur
rent band it opens a position in the direction of the newly dominant band and closes any opposite exposure.

## Details

- **Entry Criteria**:
  - **Long**: The previous finished signal bar shows the current band above the delayed band, and on the evaluated bar the curren
t band falls below or touches the delayed band. Long entries can be disabled with `AllowBuyEntry`.
  - **Short**: The previous finished signal bar shows the current band below the delayed band, and on the evaluated bar the curre
nt band rises above or touches the delayed band. Short entries can be disabled with `AllowSellEntry`.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Long positions close when the delayed band overtakes the current band (`AllowBuyExit`) or when stop-loss/take-profit limits a
re triggered.
  - Short positions close when the current band overtakes the delayed band (`AllowSellExit`) or when stop-loss/take-profit limits 
are triggered.
- **Stops**: Uses absolute price offsets specified by `StopLossPoints` and `TakeProfitPoints`. If either value is set to zero, tha

t limit is ignored.
- **Filters**:
  - `SignalBar` selects how many completed candles back the crossing logic is evaluated.
  - `CandleType` controls the timeframe used for all calculations.

## Parameters

- `CandleType` – Candle series used for the indicator (default: 1-hour candles).
- `Length` – Swing period (`SSP`) passed to the SilverTrend CrazyChart indicator.
- `KMin` – Lower channel coefficient controlling the distance of the delayed band.
- `KMax` – Upper channel coefficient controlling the distance of the current band.
- `SignalBar` – Number of finished candles back used for evaluating the cross (equivalent to the original `SignalBar`).
- `AllowBuyEntry` / `AllowSellEntry` – Toggle long/short entries.
- `AllowBuyExit` / `AllowSellExit` – Toggle closing of existing long/short positions.
- `StopLossPoints` – Absolute price distance from entry for long stop-loss and short take-profit.
- `TakeProfitPoints` – Absolute price distance from entry for long take-profit and short stop-loss.
- `Volume` – Inherited strategy volume that defines the base order size.

## Indicator Logic

The bundled `SilverTrendCrazyChartIndicator` reproduces the original MQL buffers:

- `Length`, `KMin`, and `KMax` compute a swing channel from the highest high and lowest low over the lookback window.
- The "current" band corresponds to buffer 0 in MetaTrader and reacts immediately to the latest bar.
- The "delayed" band is buffer 1, which shifts the current band by `Length + 1` bars to match the original drawing logic.

A buy is triggered when the delayed band, acting as the trend filter, crosses above the current band, while a sell appears when t
he relationship inverts. The `SignalBar` parameter ensures only completed candles participate in the decision, matching the behav
ior of the source expert.
