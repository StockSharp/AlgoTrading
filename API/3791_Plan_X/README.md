# Plan X Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Plan X breakout strategy replicates the MetaTrader expert advisor "plan x" by Peter Ingram. It focuses on the London late-morning session and waits for price to break away from a reference candle before entering. Only one net position can be open at a time, and risk is controlled through pip-based stops that trail behind the trade as it moves in favor.

## Trading Logic

1. **Session Anchor**
   - The strategy watches 15-minute candles.
   - At the configured session start hour (default 11:00), it records the close of that candle. This close acts as the anchor price for the rest of the session.
   - Trading is only considered after at least one additional candle has closed and before the session end hour (default 15:00).

2. **Entry Conditions**
   - **Long**: When the latest finished candle closes more than `LongTargetPips` (default 25 pips) above the anchor close and no position is open.
   - **Short**: When the latest finished candle closes more than `ShortTargetPips` (default 20 pips) below the anchor close and no position is open.
   - All comparisons are done in pip units derived from the instrument tick size.

3. **Position Management**
   - A fixed initial stop-loss equal to `InitialStopPips` (default 25 pips) is set relative to the entry price.
   - The stop converts into a trailing stop once the trade gains at least `TrailTriggerPips` (default 10 pips).
   - Each time price advances by another `TrailTriggerPips`, the stop is moved by `TrailStepPips` (default 5 pips) further in the profitable direction.
   - If price hits the stop, the position is closed at market.

4. **Volume**
   - Orders use the `TradeVolume` parameter (default 0.1 lots). Adjust to match the security contract size.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `TradeVolume` | Market order volume used for entries and exits. | 0.1 |
| `LongTargetPips` | Breakout distance above the anchor required for long entries. | 25 |
| `ShortTargetPips` | Breakout distance below the anchor required for short entries. | 20 |
| `InitialStopPips` | Distance from entry price to the protective stop-loss. | 25 |
| `TrailTriggerPips` | Profit in pips needed before the trailing stop activates or advances. | 10 |
| `TrailStepPips` | Pip increment applied to the trailing stop each time it moves. | 5 |
| `SessionStartHour` | Decimal hour indicating when the anchor candle begins (e.g., `11.0`, `11.5`). | 11.0 |
| `SessionEndHour` | Decimal hour after which no new entries are taken. Must be later than `SessionStartHour`. | 15.0 |
| `CandleType` | Candle series used for evaluations. Defaults to 15-minute candles. | 15-minute |

## Notes

- The pip size adapts automatically based on the instrument's `PriceStep` and decimal precision (3 or 5 decimals receive a 10x multiplier).
- The strategy expects a continuous intraday market; on instruments with daily gaps, re-anchor behavior occurs each trading day.
- Because StockSharp strategies use net positions, the conversion assumes only one open direction at a time. This mirrors the original expert's default behavior when no hedging is active.

## Files

- `CS/PlanXStrategy.cs` – C# implementation of the Plan X breakout logic for StockSharp.

