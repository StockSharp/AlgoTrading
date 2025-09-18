# Contrarian Trade MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A weekly contrarian system converted from the original MQL "Contrarian_trade_MA" expert advisor. The strategy analyses weekly candle extremes together with a simple moving average to fade stretched moves at the start of a new week.

## Trading Logic

- **Data Source**: Weekly candles provided by the `CandleType` parameter (defaults to a 7-day timeframe).
- **Historical Extremes**: `Highest` and `Lowest` indicators track the high and low of the previous `CalcPeriod` completed weeks, excluding the currently evaluated candle.
- **Moving Average Filter**: A simple moving average of length `MaPeriod` applied to weekly closes acts as a directional filter.
- **Entry Rules**:
  - **Buy** when the previous week's close is higher than the tracked high (`highest < previousClose`) or when the moving average is above the current weekly open.
  - **Sell** when the previous week's close is lower than the tracked low (`lowest > previousClose`) or when the moving average is below the current weekly open.
  - Only one position can be open at any time; opposing signals are ignored until the existing trade is closed.
- **Exit Rules**:
  - The position is closed after being held for seven days (604,800 seconds) regardless of direction.
  - A protective stop is evaluated on every completed weekly candle. The stop distance is calculated from `StopLossPoints * PriceStep` (falls back to `1` if the instrument metadata does not specify a step).

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `CalcPeriod` | `4` | Number of completed weeks used to compute the highest high and lowest low. |
| `MaPeriod` | `7` | Period of the simple moving average applied to weekly closes. |
| `StopLossPoints` | `300` | Distance from the entry price to the stop-loss, measured in price steps. Set to `0` to disable the stop. |
| `Volume` | `0.5` | Order size in lots submitted by `BuyMarket`/`SellMarket`. |
| `CandleType` | `7 days` | Timeframe for the candles driving all calculations. |

## Additional Notes

- The strategy automatically retrieves the price step from `Security.PriceStep`. Provide this value in instrument metadata for accurate stop-loss placement.
- `StartProtection()` is enabled to track unexpected position changes performed outside of the strategy.
- Because the logic operates on completed weekly candles, fills are simulated at the weekly close of the signal bar when running in testing mode.
