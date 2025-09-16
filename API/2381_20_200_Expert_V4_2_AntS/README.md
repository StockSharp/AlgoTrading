# 20/200 Expert v4.2 AntS Strategy

This strategy opens at most one position per day at a user-defined hour. It compares the open price of two past bars (T1 and T2). If the earlier bar is higher than the later one by DeltaShort pips, it opens a short position. If the later bar is higher by DeltaLong pips, it opens a long position.

Position volume can be fixed or calculated automatically from account balance. When the balance decreases compared to the previous trade, the lot is multiplied by BigLotSize.

Each trade uses its own take-profit and stop-loss in pips. Additionally, a maximum holding time (MaxOpenTime) closes the trade after the specified number of hours.

## Parameters

- `CandleType` – timeframe of processed candles (default 1 hour).
- `TradeHour` – hour of the day when entry conditions are checked.
- `T1`, `T2` – bar shifts for comparing open prices.
- `DeltaLong`, `DeltaShort` – minimal open price difference in pips.
- `TakeProfitLong`, `StopLossLong` – protection for long trades in pips.
- `TakeProfitShort`, `StopLossShort` – protection for short trades in pips.
- `Lot` – base trading volume.
- `AutoLot` – enable automatic lot calculation.
- `BigLotSize` – multiplier applied after loss.
- `MaxOpenTime` – maximum time in hours to keep a position.
