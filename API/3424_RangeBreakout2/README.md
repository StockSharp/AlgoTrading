# RangeBreakout2 Strategy

## Overview

The **RangeBreakout2 Strategy** is a StockSharp port of the MetaTrader expert advisor "RangeBreakout2". The algorithm prepares a price range at configurable times (weekly, daily or continuously) and opens a single market order once bid/ask quotes escape that range. After each trade the range preparation cycle restarts. The implementation reproduces the original money-management rules (constant, linear, martingale and Fibonacci scaling) and the optional expansion of the take-profit distance after a losing trade.

The strategy works with a single security and relies on the best bid/ask quotes. Ensure the adapter provides up-to-date order book data so that breakout detection remains responsive.

## Trading Logic

1. **Scheduling** – At the configured time the strategy records the current ask price as the center of the setup and derives the upper/lower breakout levels from the raw range.
2. **Range calculation** – The raw range is obtained from one of three modes:
   - **ATR** – Multiplies the latest Average True Range value by `AtrPercentage`.
   - **Percent** – Uses `PricePercentage` percent of the current ask price.
   - **Fixed** – Converts `FixedRangePoints` price steps into an absolute distance.
3. **Breakout detection** – While in the `Setup` phase the strategy watches the best bid/ask. When the ask moves above the upper level or the bid falls below the lower level, it submits a market order.
4. **Entry type** – `TradeMode` selects between breakout (`Stop`), fade (`Limit`) or random behaviour. Random mode chooses either breakout or fade on each entry.
5. **Protection** – Stop-loss and take-profit offsets are derived from the raw range. If the previous trade closed with a loss and `RangeMultiplier` is greater than 1, the take-profit distance is expanded by that multiplier.
6. **Money management** – Order volume is computed from free portfolio capital (`CurrentValue - BlockedValue`) and the selected lot mode:
   - **Constant** – Always uses the base volume.
   - **Linear** – Increases linearly after each loss.
   - **Martingale** – Multiplies the previous volume by `LotMultiplier` after a loss.
   - **Fibonacci** – Grows following the Fibonacci sequence after losses.

Once the position is closed the strategy resets to the standby phase and waits for the next schedule trigger.

## Parameters

| Group | Name | Description | Default |
|-------|------|-------------|---------|
| Schedule | `Periodicity` | Range preparation frequency: Weekly, Daily or NonStop. | `Weekly` |
| Schedule | `Day` | Trading day used when `Periodicity` = Weekly. | `Monday` |
| Schedule | `Hour` | Hour of day when the setup is built (MetaTrader-style adjustment: stored value + 1, capped to 0 if ≥ 23). | `0` |
| Range | `RangeMode` | Raw range calculation method (ATR / Percent / Fixed). | `Atr` |
| Range | `AtrPercentage` | Percentage multiplier applied to the ATR value. | `50` |
| Range | `AtrLength` | Number of candles used in the ATR indicator. | `20` |
| Range | `PricePercentage` | Percentage of the current ask price used when `RangeMode = Percent`. | `1` |
| Range | `FixedRangePoints` | Fixed range expressed in price steps when `RangeMode = Fixed`. | `1000` |
| Trading | `RangePercentage` | Percentage of the raw range applied to breakout levels. | `100` |
| Trading | `TradeMode` | Entry style: Stop (breakout), Limit (fade) or Random. | `Stop` |
| Trading | `TakeProfitPercentage` | Take-profit distance as a percentage of the (optionally expanded) range. | `100` |
| Trading | `StopLossPercentage` | Stop-loss distance as a percentage of the base range. | `100` |
| Risk | `LotMode` | Lot management scheme (Constant / Linear / Martingale / Fibonacci). | `Martingale` |
| Risk | `MarginPercentage` | Portion of free capital reserved for the base order volume. | `10` |
| Risk | `LotMultiplier` | Multiplier applied in martingale-like scaling modes. | `2` |
| Risk | `RangeMultiplier` | Take-profit multiplier applied after a losing trade. | `1` |
| Data | `SignalCandleType` | Candle type used to check scheduling conditions. | `1m time-frame` |
| Data | `AtrCandleType` | Candle type used for ATR calculation. Only requested when `RangeMode = Atr`. | `1d time-frame` |

## Implementation Notes

- The strategy requires live bid/ask updates; without them breakout detection will not trigger.
- Base volume calculations rely on portfolio equity (`CurrentValue - BlockedValue`). When the connector does not supply these fields, the volume falls back to the exchange minimum.
- Protective orders are placed via `SetStopLoss` and `SetTakeProfit`. The resulting position (after the new trade) is passed so that the base class can manage combined protection for scaling scenarios.
- The ATR fallback mimics the original expert advisor: if the indicator is not ready the range defaults to 1% of the current ask price.
- Random trade mode uses the .NET `Random` class seeded on strategy construction. Two consecutive breakouts may therefore choose different entry types.

## Usage Tips

1. Configure the `SignalCandleType` to match the desired resolution of schedule checks. A one-minute candle stream closely reproduces the tick-driven behaviour of the MQL version.
2. For weekly schedules make sure the server time zone matches the expectation from the original EA.
3. Monitor the effect of `RangeMultiplier` when using martingale-like lot modes: enlarging the take-profit distance together with growing volumes increases exposure after losing streaks.
4. Because stop-loss and take-profit distances are derived from the raw range, large `RangePercentage` values lead to equally large protective offsets.
