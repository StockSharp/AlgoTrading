# Range Breakout Weekly Strategy (ID 3412)

The **Range Breakout Weekly Strategy** is a StockSharp high-level API conversion of the MetaTrader 5 expert advisor `RangeBreakout.mq5`. The system prepares breakout levels once per week using a configurable weekday and hour, then opens a single trade when price breaks above or below the calculated range. Martingale-style position sizing and loss-compensation logic mirror the original script, while the implementation leverages StockSharp subscriptions for candles, Level 1 quotes, and indicator binding.

## Trading Logic

1. **Weekly preparation window.** At the close of the specified hourly candle on the configured weekday, the strategy records the candle's close as the reference price and transitions from *Standby* to *Setup* phase.
2. **Range calculation.**
   - The primary range is derived from a 20-period daily Average True Range (ATR). The ATR value is multiplied by `ATR Percentage` and normalised to the instrument's tick size.
   - If ATR data is missing, the algorithm falls back to multiplying the current ask price by `Price Percentage`.
3. **Protective levels.**
   - Upper and lower breakout triggers are placed one range above and below the reference close.
   - Take-profit and stop-loss offsets are computed as percentages of the range. When compensation is active after a loss, the take-profit is replaced by the accumulated compensation offset and the stop-loss is widened by the same amount, just like the MetaTrader logic.
4. **Execution.**
   - While in *Setup*, the strategy listens to Level 1 quotes. A break above the upper trigger enters a long position; a drop below the lower trigger opens a short position. Orders are sent as market orders with tick-aligned price checks.
   - Once a position is active (*Trade* phase), Level 1 quotes are continuously monitored. Hitting the protective stop or target closes the position with a market order.
5. **Martingale recovery.**
   - After a losing exit, the next trade size doubles and the loss offset is added to the compensation buffer so that the following target aims to recover the cumulative loss.
   - A winning exit resets both the multiplier and the compensation buffer to their initial values.
6. **Daily reset.** After a trade concludes, the strategy returns to the *Standby* phase and waits until the next eligible weekday/hour combination to prepare a new setup.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Trading Day` | Monday | Weekday used to measure the breakout reference candle. Weekend selections are automatically remapped to Monday, matching the original warning behaviour. |
| `Start Hour` | 0 | Hour (0-23) whose closing candle serves as the reference. Optimisable to cover various session openings. |
| `Price Percentage` | 1.0 | Fallback percentage of the ask price used to compute the range when ATR data is missing. |
| `ATR Percentage` | 100 | Multiplier applied to the daily ATR value to obtain the breakout range. |
| `Take Profit Percentage` | 100 | Percentage of the range added beyond the entry to define the take-profit price. Overridden by the compensation buffer after consecutive losses. |
| `Stop Loss Percentage` | 100 | Percentage of the range subtracted from the entry to set the stop-loss price. The compensation buffer widens this distance after losses. |
| `Base Volume` | 0.1 | Initial trading volume before martingale scaling. The value is automatically rounded to the instrument's volume step and clamped by minimum/maximum constraints. |
| `ATR Period` | 20 | Number of daily candles supplied to the ATR indicator. |
| `Hour Candle Type` | 1-hour time frame | Candle subscription used for detecting the preparation window. |
| `ATR Candle Type` | 1-day time frame | Candle subscription that feeds the ATR indicator. |

## Implementation Notes

- **Data subscriptions.** The strategy subscribes to hourly candles for scheduling, daily candles for the ATR calculation, and Level 1 data for bid/ask monitoring. The high-level `Bind` API is used to stream indicator values without manual buffer handling.
- **Tick alignment.** All price levels (reference, triggers, stop-loss, take-profit) are normalised through `Security.ShrinkPrice` to respect tick size constraints, mimicking MetaTrader's `NormalizeDouble` behaviour.
- **Volume handling.** Trade volumes are rounded to the instrument's `VolumeStep` and constrained by `VolumeMin`/`VolumeMax` before order submission, replicating the original lot sanitisation.
- **Phase machine.** Internal phases (`Standby`, `Setup`, `Trade`) replace the original enum logic, ensuring a single trade per preparation cycle. After each exit the state resets to `Standby` until the next qualifying candle occurs.
- **Compensation buffer.** The `compensationOffset` field stores the accumulated loss distance expressed in price units. When active, the next setup replaces the take-profit offset with this value and widens the stop by the same amount, mirroring the MetaTrader formula that converts past monetary loss into price distance.
- **Logging.** Selecting Saturday or Sunday triggers an informational log and automatically switches the working day to Monday, consistent with the warning shown by the MQL version.

## Usage Tips

1. Align `Trading Day` and `Start Hour` with the session that generates meaningful ranges (e.g., Asian range breakout or London open breakout).
2. Calibrate `ATR Percentage`, `Take Profit Percentage`, and `Stop Loss Percentage` together. Increasing the range multiplier produces wider triggers and slower trades, while adjusting profit/loss percentages modifies the reward-to-risk ratio.
3. Enable optimisation on `Start Hour`, `Base Volume`, or the percentage parameters to reproduce parameter sweeps from the original expert advisor.
4. Monitor the cumulative exposure created by the martingale multiplier. Consider lowering `Base Volume` when running on highly leveraged accounts.
5. The strategy is designed for a single instrument. Deploy multiple copies with different securities or session settings to diversify coverage.

## Conversion Coverage

- ✅ Preserved weekly scheduling, range calculations, protective levels, and martingale behaviour from `RangeBreakout.mq5`.
- ✅ Replaced MetaTrader-specific API calls (`iATR`, `CopyBuffer`, `OrderSend`, etc.) with idiomatic StockSharp abstractions (`SubscribeCandles`, `AverageTrueRange`, `BuyMarket`/`SellMarket`).
- ✅ Implemented English inline comments and extensive documentation as requested.
- ✅ Left test projects untouched and did not create a Python variant, complying with the task constraints.
