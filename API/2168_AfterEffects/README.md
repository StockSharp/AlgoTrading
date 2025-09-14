# AfterEffects Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The AfterEffects strategy is based on the idea that market prices may show aftereffects.
It calculates a signal using the current close price and the opens from `p` and `2p` bars ago:

`signal = Close - 2 * Open[p] + Open[2p]`

A positive signal opens a long position, while a negative signal opens a short position.
Setting `Random` to true inverts the signal.

Once in a position the strategy places a stop loss `StopLoss` points away from the entry.
When price moves `2 * StopLoss` points in the favorable direction:

- if the signal changes sign the position is reversed by trading double volume;
- otherwise the stop loss is trailed to the new level.

## Details

- **Entry Criteria**: `signal > 0` for long, `signal < 0` for short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop loss.
- **Stops**: Trailing stop.
- **Default Values**:
  - `StopLoss` = 500
  - `Period` = 3
  - `Random` = false
  - `Volume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Custom formula
  - Stops: Trailing
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
