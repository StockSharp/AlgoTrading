# XWAMI Multi-Layer MMRec Strategy

This strategy ports the original **Exp_XWAMI_NN3_MMRec.mq5** expert advisor into StockSharp. Three independent layers (A/B/C) run the XWAMI momentum indicator on different timeframes and combine their signals inside a single netted position. Each layer emulates the corresponding MagicNumber from the MetaTrader version, including its money-management recounter and protective levels.

## Trading logic

* For every layer a momentum series is computed as `price - price[iPeriod]` using the selected applied price. The difference is passed through four sequential smoothers (configurable methods and lengths) to obtain the "up" and "down" lines of the XWAMI indicator.
* Signals are evaluated on the `SignalBar` shift. When the previous bar had `up > down`, shorts from that layer are closed and a long entry is allowed if the most recent bar shows `up <= down`. When the previous bar had `up < down`, longs are closed and a short entry is allowed when `up >= down`.
* Before opening in a new direction the strategy flatters all opposite positions from other layers to respect StockSharp's netting model. This mirrors the behaviour of closing an opposite magic-number trade in the MQL code.
* Optional stop-loss and take-profit levels (expressed in price points) are checked on every completed candle using the candle's high/low. If hit they force an immediate exit for that layer.

## Money management recounter

Each layer keeps a rolling history of its most recent trades. Whenever the number of losses inside the configured lookback reaches the *LossTrigger*, the position size switches from the normal volume to the reduced ("Small") volume. Successful trades or smaller loss counts revert to the normal size. Buy and sell directions maintain their own counters, exactly as in the original MMRec helper.

## Parameters

The strategy exposes the full parameter set of the MQL expert:

* `Layer?CandleType` – candle type (time frame) used by the layer (defaults: A=8h, B=4h, C=1h).
* `Layer?Period` – lag used to build the momentum series.
* `Layer?Method1..4`, `Layer?Length1..4`, `Layer?Phase1..4` – smoothing configuration for the four XWAMI stages.
* `Layer?AppliedPrice` – applied price formula (close, open, weighted, Demark, etc.).
* `Layer?SignalBar` – shift of the signal bar (0 = current, 1 = last closed bar, default 1).
* `Layer?AllowBuy/SellOpen` and `Layer?AllowBuy/SellClose` – permissions for entries and exits.
* `Layer?NormalVolume`, `Layer?SmallVolume` – trade size in lots (or units) for normal and reduced modes.
* `Layer?BuyTotalTrigger`, `Layer?BuyLossTrigger`, `Layer?SellTotalTrigger`, `Layer?SellLossTrigger` – MMRec counters controlling the switch to the reduced volume.
* `Layer?StopLossPoints`, `Layer?TakeProfitPoints` – protective levels in price points (0 disables the level).

## Notes

* The StockSharp version uses a single net position. When two layers disagree, opposite positions are closed before entering the new one, preserving the intended order of signals while avoiding hedging.
* The Tillson T3 stage is implemented directly in C# to keep parity with the original smoothing algorithm. Other smoothing modes map to StockSharp's built-in indicators (SMA, EMA, SMMA/RMA, LWMA, Jurik).
* Because historical trade queries differ between platforms, the MMRec logic tracks completed trades inside the strategy and reproduces the same thresholds without scanning the terminal history.
