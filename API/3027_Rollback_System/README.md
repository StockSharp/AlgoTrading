# Rollback System Strategy

This strategy is a C# conversion of the MetaTrader 5 expert advisor **"Rollback system"**. It keeps the original idea of
trading at the very beginning of a new trading day, evaluating the last 24 hourly candles to detect whether the market delivered
an extended move that is likely to retrace.

## Trading logic

1. The strategy works on an hourly timeframe (`CandleType`, default 1 hour).
2. Signals are evaluated only once per day when the new day starts (`00:00` – `00:03`). The filter skips Monday and Friday
   sessions exactly like the MQL version.
3. Before opening a position the algorithm ensures that no other trades are active.
4. For every trading day the following values are calculated from the last 24 closed candles:
   - `Open_24_minus_Close_1` – distance between the open price 24 bars ago and the latest close.
   - `Close_1_minus_Open_24` – inverse distance showing the net day change.
   - `Close_1_minus_Lowest` – how far the close is from the lowest low of the day.
   - `Highest_minus_Close_1` – how far the close is from the highest high of the day.
5. Entry rules (expressed in price units converted from the pip parameters):
   - **Long #1** – previous day fell (`Open_24_minus_Close_1` above the `ChannelOpenClosePips` threshold) and the close is still
     near the extreme low (`Close_1_minus_Lowest` below `RollbackPips - ChannelRollbackPips`).
   - **Long #2** – previous day rallied (`Close_1_minus_Open_24` above the channel threshold) but the market closed far below the
     daily high (`Highest_minus_Close_1` greater than `RollbackPips + ChannelRollbackPips`).
   - **Short #1** – previous day rallied and the close finished near the daily high (`Highest_minus_Close_1` below
     `RollbackPips - ChannelRollbackPips`).
   - **Short #2** – previous day sold off and the close recovered far above the daily low (`Close_1_minus_Lowest` above
     `RollbackPips + ChannelRollbackPips`).
6. Orders are executed with `BuyMarket`/`SellMarket` using the configured trade volume. Stop-loss and take-profit levels are
   derived from `StopLossPips` and `TakeProfitPips` (both zero disable the respective protection).
7. Protective levels are monitored on every finished candle. If price breaches a level intrabar the strategy closes the position
   using a market order, replicating the behaviour of the original MQL expert advisor that submitted hard stops.

## Parameter conversion from pips

MetaTrader 5 multiplies pip values by 10 on 3- and 5-digit symbols. The conversion logic is preserved: the strategy takes the
instrument's `PriceStep` and applies a tenfold multiplier when the detected number of decimal digits equals 3 or 5. This keeps the
entry thresholds, stop-loss and take-profit distances consistent with the MQL implementation across typical FX symbols.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Trade size used for market orders. |
| `StopLossPips` | Stop-loss distance in pips. Set to zero to disable. |
| `TakeProfitPips` | Take-profit distance in pips. Set to zero to disable. |
| `RollbackPips` | Base rollback requirement used by all signals. |
| `ChannelOpenClosePips` | Minimum difference between the previous day's open and close. |
| `ChannelRollbackPips` | Tolerance added/subtracted from the rollback check. |
| `CandleType` | Working candle type, defaults to hourly bars. |

## Notes

- The MQL version painted rectangles on the chart for visual reference. The StockSharp port keeps the trading logic only.
- Risk management is implemented with in-strategy monitoring instead of server-side protective orders because the high-level API
  manages positions directly.
- When optimising, adjust the pip thresholds and volume to suit the target instrument and broker tick size.
