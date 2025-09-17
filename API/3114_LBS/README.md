# LBS Strategy

The **LBS Strategy** is a direct conversion of the MetaTrader 5 expert advisor "LBS (barabashkakvn's edition)". The original system watches for breakouts of the previous candle during a configurable trading window and places stop orders at both extremes. The StockSharp port keeps the same trade management rules while using the high-level API (`SubscribeCandles`, `SubscribeLevel1`, `BuyStop`/`SellStop`) for clarity and reliability.

## Trading logic

1. The strategy monitors finished candles of the selected timeframe (`CandleType`).
2. When the close time of the candle matches any of the enabled trading hours (`Hour1`, `Hour2`, `Hour3`), the algorithm calculates breakout levels:
   - The buy stop is placed at the higher of the candle high and the current ask plus a freeze buffer.
   - The sell stop is placed at the lower of the candle low and the current bid minus the same buffer.
   - The buffer reproduces MetaTrader's `SYMBOL_TRADE_FREEZE_LEVEL` fallback (three spreads, but never less than ten pips).
3. If a position is opened, the opposite pending order is cancelled immediately, just like the MQL expert's `DeleteAllPendingOrders` routine.
4. Initial stop-loss prices are attached according to `StopLossPips`. Optional trailing logic (`TrailingStopPips` and `TrailingStepPips`) shifts the stop once the floating profit is larger than the configured thresholds.
5. Orders are only sent when the strategy is online, no position is open, and valid Level1 quotes are available.

## Money management

`MoneyMode` mirrors the `Lot/Risk` switch from the original expert:

- **FixedLot** – the `VolumeOrRisk` parameter is interpreted as an absolute trade volume.
- **RiskPercent** – the strategy converts `VolumeOrRisk` into a fraction of the portfolio value. The risk amount is divided by the distance between the entry price and the protective stop (in price steps) to obtain the order volume. When this mode is used the stop-loss must be enabled; otherwise the order is skipped.

All volumes are normalised to the instrument's minimum, maximum and step constraints to avoid broker rejections.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `StopLossPips` | 50 | Distance to the fixed stop in pips. Zero disables both the initial stop and trailing module. |
| `TrailingStopPips` | 5 | Trailing-stop distance in pips. Zero disables trailing. |
| `TrailingStepPips` | 15 | Additional profit (in pips) required before the trailing stop is moved. Must stay positive when trailing is enabled. |
| `MoneyMode` | `FixedLot` | Selects between fixed volume and risk-percentage sizing. |
| `VolumeOrRisk` | 1.0 | Lot size in `FixedLot` mode or risk percentage in `RiskPercent` mode. |
| `Hour1` | 10 | First trading hour. Set to `0` to disable. |
| `Hour2` | 11 | Second trading hour. Set to `0` to disable. |
| `Hour3` | 12 | Third trading hour. Set to `0` to disable. |
| `CandleType` | 1-hour time frame | Candle series used to detect breakouts; adjust to mirror the chart timeframe from MetaTrader. |

## Notes

- Hour comparisons use the candle close time, which corresponds to the moment when MetaTrader's `TimeCurrent()` equals the start of the next bar.
- The freeze/stop level approximation guarantees that stop orders are never closer than ten pips to the current bid/ask, preventing the most common MetaTrader errors.
- Trailing stops are updated on every Level1 tick, ensuring behaviour close to the tick-driven `OnTick` handler in the original expert.
- Risk-based sizing uses `Portfolio.CurrentValue` when available and falls back to `Portfolio.BeginValue` otherwise.

## Usage tips

1. Attach the strategy to an instrument and pick the same timeframe that was used in MetaTrader.
2. Configure the trading hours according to the session you want to trade (setting them to `0` disables that slot).
3. Select `RiskPercent` mode if you want automatic scaling; make sure `StopLossPips` is positive.
4. For fixed-lot trading, keep `MoneyMode` at `FixedLot` and set `VolumeOrRisk` to the desired size.
5. Start the strategy. It will place two pending orders at the next configured hour and maintain the protective stop automatically.
