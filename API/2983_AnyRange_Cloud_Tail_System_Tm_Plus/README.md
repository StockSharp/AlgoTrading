# AnyRange Cloud Tail System Tm Plus Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the behaviour of the **Exp_i-AnyRangeCldTail_System_Tm_Plus.mq5** expert using StockSharp's high level API. It builds a custom intraday range between two user-defined times, waits for breakouts beyond that range, and schedules orders a configurable number of bars after the breakout so that signals are aligned with the original MQL timing logic.

The strategy is designed for both long and short trading. It exposes parameters that control breakout permissions, stop-loss/take-profit distances in price steps, the holding period, and the indicator calculation window. In addition, a time-based exit closes positions that remain open longer than the configured number of minutes, matching the protective logic of the source expert advisor.

## Trading Logic

1. **Range Construction**
   - Two timestamps (`RangeStartTime` and `RangeEndTime`) define the session window used to compute the reference range.
   - For each completed day the strategy records the highest high and lowest low between these timestamps. If `RangeStartTime` is greater than `RangeEndTime`, the window automatically spans across midnight, just like the original indicator.
   - The latest completed range is reused until a new daily range is completed.

2. **Breakout Detection**
   - Each finished candle is compared with the stored range.
   - Candles closing above the range high receive the same colour codes (2 or 3) as the MQL indicator, while candles closing below the range low receive codes 0 or 1. Candles inside the range are tagged with code 4 (no signal).
   - The `SignalBar` parameter shifts the inspection point: the strategy evaluates the candle that is `SignalBar + 1` bars old and confirms that the more recent candle (`SignalBar`) does not carry the same colour. This reproduces the delayed confirmation used by the EA to trigger orders one bar after the breakout candle.

3. **Entries**
   - **Long**: permitted when `AllowBuyEntry` is true and a bullish colour (2 or 3) is detected on the signal bar while the following bar does not repeat the breakout colour.
   - **Short**: permitted when `AllowSellEntry` is true and a bearish colour (0 or 1) is detected on the signal bar while the following bar does not repeat the breakout colour.
   - If an opposite position is open, its volume is added to the new market order so that the position flips immediately, emulating the behaviour of the helper functions in `TradeAlgorithms.mqh`.

4. **Exits**
   - **Opposite Signal**: if `AllowBuyExit` is enabled, a bearish colour (0 or 1) on the signal bar closes long positions. If `AllowSellExit` is enabled, a bullish colour (2 or 3) closes short positions.
   - **Time Exit**: when `UseTimeExit` is true, positions are liquidated after `ExitAfterMinutes` minutes from entry, matching the MQL loop that scans positions and closes them after `nTime` minutes.
   - **Stops/Targets**: optional stop-loss and take-profit protections are configured via `StopLossPoints` and `TakeProfitPoints`. Values are converted into price distances using the security's price step, mirroring the original point-based configuration.

5. **Risk Controls**
   - Orders use the configured `OrderVolume` (base size expressed in instrument volume units). The order size is applied on every `BuyMarket`/`SellMarket` call and adjusted when flipping positions.
   - Stop-loss and take-profit are managed by the built-in `StartProtection` helper, which registers OCO protections right after the strategy starts.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Base order size used for new positions. | `0.1` |
| `AllowBuyEntry` | Allow long entries on bullish breakouts. | `true` |
| `AllowSellEntry` | Allow short entries on bearish breakouts. | `true` |
| `AllowBuyExit` | Close long positions on bearish breakouts. | `true` |
| `AllowSellExit` | Close short positions on bullish breakouts. | `true` |
| `UseTimeExit` | Enable the time-based exit. | `true` |
| `ExitAfterMinutes` | Holding time in minutes before the time exit fires. | `1500` |
| `StopLossPoints` | Stop-loss distance in price steps. Use `0` to disable. | `1000` |
| `TakeProfitPoints` | Take-profit distance in price steps. Use `0` to disable. | `2000` |
| `SignalBar` | Number of bars back inspected for breakout detection (matches the MQL `SignalBar`). | `1` |
| `RangeLookbackDays` | Maximum number of past sessions scanned to find a completed range. Set to `0` to always use the most recent range only. | `1` |
| `RangeStartTime` | Start of the range-building window (TimeSpan). | `02:00` |
| `RangeEndTime` | End of the range-building window (TimeSpan). | `07:00` |
| `CandleType` | Candle data type/timeframe used for calculations. | `30 minutes` |

## Implementation Notes

- The class uses `SubscribeCandles` and the event-driven `WhenNew` pipeline to process finished candles only, ensuring decisions match the MQL expert that relied on `IsNewBar` checks.
- Range values are stored in lightweight structs and the algorithm avoids LINQ over full collections to comply with the project guidelines.
- The time exit stores the entry timestamp for the currently open direction, mirroring how the source code iterated through open positions.
- Order volume is synchronised with the base `Strategy.Volume` property so the StockSharp UI reflects the configured size.
- The code contains English comments that explain each major section to facilitate maintenance and further customisation.

## Usage Tips

- Ensure that the data feed provides candles that align with the chosen `CandleType`. The breakout detection relies on completed candles; tick-based or partially formed bars should not be processed.
- When trading markets with different trading sessions, adjust `RangeStartTime` and `RangeEndTime` to cover the accumulation period that best matches the underlying instrument.
- If the instrument has an irregular price step, verify the `StopLossPoints`/`TakeProfitPoints` conversion by inspecting the generated protective orders in the chart or order log.
- Reduce `ExitAfterMinutes` when operating on faster timeframes to avoid holding positions longer than intended.
