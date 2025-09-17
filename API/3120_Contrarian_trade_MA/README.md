# Contrarian Trade MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy recreates the MetaTrader expert advisor **"Contrarian trade MA"** using the StockSharp high-level API. It combines weekly context with a Monday-only entry filter to trade against extremes. The system waits for a new trading week, measures how far the previous week closed relative to the highest high and lowest low over the lookback window, and checks whether price opened the new week on the opposite side of a shifted moving average. If the market finishes the first daily candle of the week outside those thresholds, a contrarian position is opened.

The logic relies on finished candles only. A daily series (default) drives entries and exits, while a weekly series supplies the extreme levels and the moving-average signal. Each time a Monday candle completes, the strategy evaluates whether the previous week ended above the recent high band or below the recent low band, or whether the previous moving average value stands on the other side of the current weekly open. The assumption is that such overextended moves tend to mean-revert during the week.

## How it works

1. Weekly candles feed two indicators:
   - `Highest`/`Lowest` find the extreme high and low over `CalcPeriod` weeks.
   - A configurable moving average (`MaPeriod`, `MaMethod`, `MaShift`, `AppliedPrice`) processes the same weekly candles.
2. Daily candles (or any selected `TradeCandleType`) trigger trading decisions once they finish.
3. On the first completed candle whose `OpenTime.DayOfWeek == Monday`, the strategy evaluates entry conditions:
   - **Long** if the previous weekly close is above the highest high of the lookback or if the previous MA value is greater than the current weekly open (meaning price opened below the MA).
   - **Short** if the previous weekly close is below the lowest low of the lookback or if the previous MA value is less than the current weekly open (price opened above the MA).
4. Orders are sent with `BuyMarket` or `SellMarket` using the strategy volume and no averaging. Only one position can be open at a time.

## Exit management

- A fixed stop-loss distance is calculated as `StopLossPips * Security.PriceStep`. When enabled (> 0), the strategy monitors daily candle highs and lows; if price touches the stop level within the day, the position is closed at market.
- A time-based exit closes any open position once seven days have passed since the entry (`604800` seconds in the original EA). The check is performed on each finished daily candle.
- The strategy never opens a new trade until the previous one is fully closed.

## Indicators and data

- **Weekly extremes:** `Highest` and `Lowest` indicators attached to the `MaCandleType` series (default 1-week candles).
- **Weekly moving average:** `Simple`, `Exponential`, `Smoothed`, or `LinearWeighted` methods are available. The moving average can be shifted forward by `MaShift` bars to mimic the MetaTrader setting and can consume different price sources (`AppliedPrice`).
- **Primary timeframe:** `TradeCandleType` defines which candles drive trade timing; the default is daily candles so entries are evaluated after the first day of the trading week has closed.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CalcPeriod` | `int` | `4` | Number of higher-timeframe candles used to calculate the highest high and lowest low. |
| `StopLossPips` | `int` | `300` | Stop-loss distance in price steps. Set to `0` to disable the protective stop. |
| `MaPeriod` | `int` | `7` | Length of the weekly moving average. |
| `MaShift` | `int` | `0` | Forward shift of the moving average in bars. Mirrors the MetaTrader MA shift parameter. |
| `MaMethod` | `MovingAverageMethod` | `LinearWeighted` | Moving average calculation method (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). |
| `AppliedPrice` | `AppliedPriceType` | `Weighted` | Price source fed into the moving average (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `TradeCandleType` | `DataType` | `TimeSpan.FromDays(1).TimeFrame()` | Primary timeframe that triggers entries and manages stops/exits. |
| `MaCandleType` | `DataType` | `TimeSpan.FromDays(7).TimeFrame()` | Higher timeframe used for the moving average and for calculating extremes. |

## Notes

- The stop-loss distance adapts to the instrument by multiplying the pip count by `Security.PriceStep`. Instruments without a defined step will effectively disable the stop.
- Because the strategy evaluates finished candles, entries occur at the close of the Monday bar rather than the very first tick of the week. This keeps behaviour deterministic across backtests.
- The logic assumes only one open position; any open trade is closed either by the stop-loss or by the seven-day timeout before a new signal is considered.
