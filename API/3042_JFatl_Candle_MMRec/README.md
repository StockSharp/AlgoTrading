# JFatl Candle MMRec Strategy

This strategy recreates the behaviour of the original **Exp_JFatlCandle_MMRec.mq5** Expert Advisor inside the StockSharp framework.
It analyses the colour changes produced by the JFatl candle filter and combines them with an adaptive money management block
that reduces the trading size after a configurable number of recent losses.

## Trading idea

* Build synthetic candles by filtering the classic OHLC values with the Fast Adaptive Trend Line (FATL) kernel.
  The implementation uses the original 39-tap coefficient table followed by an exponential smoothing stage in order to
  approximate the Jurik moving average used in MetaTrader.
* Detect colour transitions of the synthetic candle body:
  * colour **2** (bullish) means the filtered close is above the filtered open;
  * colour **0** (bearish) means the filtered close is below the filtered open;
  * colour **1** marks a neutral body.
* A bullish colour on the bar that is `SignalBar + 1` periods old forces the strategy to flat any shorts and to prepare
  for a new long entry when the bar `SignalBar` periods old is no longer bullish.
* A bearish colour observed in the same way closes longs and enables a short entry when the more recent bar is not bearish anymore.
* Long and short positions are sized through the MMRecounter logic. When the last `TotalTrigger` trades of the
  corresponding direction include at least `LossTrigger` negative results, the strategy switches to the reduced position size.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Time-frame of the candles that are fed into the FATL filter (default: 12 hours).
| `SignalBar` | Number of completed bars to look back when reading the colour buffer. `0` means to use the current finished bar, `1` reproduces the MT5 defaults.
| `SmoothingLength` | Exponential smoothing length applied after the FATL kernel to emulate Jurik smoothing.
| `NormalVolume` | Default position size used when the recent track record is healthy.
| `ReducedVolume` | Position size applied after the MMRecounter detects too many losses.
| `BuyTotalTrigger` / `SellTotalTrigger` | Amount of historical trades (per direction) inspected by the MMRecounter.
| `BuyLossTrigger` / `SellLossTrigger` | Minimal number of losses inside the inspected window that forces the reduced position size.
| `EnableBuyEntries` / `EnableSellEntries` | Allow opening long/short positions.
| `EnableBuyExits` / `EnableSellExits` | Allow closing long/short positions when the opposite signal appears.
| `StopLossPoints` | Optional protective stop for both directions expressed in security price steps. Set to `0` to disable.
| `TakeProfitPoints` | Optional profit target in price steps. Set to `0` to disable.

## Trading rules

1. Build the filtered OHLC values and determine the candle colour at each finished bar.
2. Let `C1` be the colour of the bar `SignalBar + 1` periods ago and `C0` the colour of the bar `SignalBar` periods ago
   (for `SignalBar = 0` the current bar is used as `C0` and the previous bar as `C1`).
3. If `C1 == 2` (bullish)
   * close any short position when `EnableSellExits` is `true`;
   * open a long position with the calculated position size when `EnableBuyEntries` is `true` **and** `C0 != 2`.
4. If `C1 == 0` (bearish)
   * close any long position when `EnableBuyExits` is `true`;
   * open a short position when `EnableSellEntries` is `true` **and** `C0 != 0`.
5. Positions can also be closed by stop-loss or take-profit boundaries when the candle range touches the configured level.

## Money management

The strategy stores the profit of every completed long and short trade separately. When a new entry is considered, it scans
up to `TotalTrigger` previous trades of that direction. If at least `LossTrigger` trades within that window ended with a negative
result, the reduced volume is used; otherwise, the normal volume is traded.

## Notes

* Price-step based stop-loss and take-profit logic relies on the `Security.PriceStep` value. If the instrument does not provide it,
  a step of `1` is assumed.
* The FATL filter needs at least 39 historical candles before it becomes operational. No trades are generated until
  enough data is accumulated.
* The strategy keeps a compact trade history for the MMRecounter block; once the history exceeds 100 items the oldest records
  are discarded automatically.
