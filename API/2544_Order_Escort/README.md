# Order Escort Trailing Manager
[Русский](README_ru.md) | [中文](README_cn.md)

A utility strategy that mirrors the "Order Escort" expert advisor. It does not create new positions. Instead, it watches the existing position on the configured security and gradually shifts the protective stop (and optionally the take-profit) after each finished bar. The trailing distance grows according to a selected curve (linear, parabolic, or exponential) until the configured maximum number of bars is reached.

## Details

- **Entry Criteria**: None. The strategy assumes a position is opened manually or by another system.
- **Position Direction**: Works for both long and short positions.
- **Exit Criteria**:
  - Close the position when the trailed stop is touched.
  - Close the position when the escorted take-profit is hit.
  - Force-close the position after the configured number of bars (`CloseBar`).
- **Stops**: Automatically recalculated trailing stop based on bar count.
- **Default Values**:
  - `TargetBar` = 35
  - `DeltaPoints` = 80
  - `TrailingMode` = Linear
  - `Exponent` = 0.5
  - `EBase` = 2.718
  - `EscortTakeProfit` = true
  - `CloseBar` = 15
  - `CandleType` = 1-minute time frame
- **Filters**:
  - Category: Trade management
  - Direction: Both
  - Indicators: None
  - Stops: Trailing stop & optional take-profit escort
  - Complexity: Medium
  - Timeframe: Configurable
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Depends on initial protective orders

## How it works

1. Subscribe to the chosen candle series and count the number of finished bars since the strategy started.
2. Compute the total trailing distance in points by applying one of three progressions:
   - **Linear**: evenly increases the offset by `DeltaPoints / TargetBar` per bar.
   - **Parabolic**: uses a power curve defined by the `Exponent` parameter.
   - **Exponential**: multiplies the distance using the custom `EBase` value.
3. When a position exists, capture the initial stop and take-profit anchors using the close price of the first finished bar after entry.
4. On each finished bar, translate the trailing distance to price units (using the security price step) and update the stop and take-profit anchors.
5. If price crosses a trailed level, close the position at market. When `CloseBar` is reached, close any remaining position regardless of profit or loss.

## Notes

- Designed as a manager that can be combined with discretionary entries, signals, or other automated systems.
- Works best when the initial stop loss and take-profit are set close to the desired protective levels, because the trailing logic offsets the captured anchors.
- The price step is read from `Security.PriceStep`; if it is missing, a step of 1 is used.
