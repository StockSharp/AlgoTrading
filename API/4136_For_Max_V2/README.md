# For Max V2

## Overview
For Max V2 is a port of the MetaTrader 4 expert advisor `for_max_v2.mq4`. The strategy waits for specific two-candle engulfing patterns and then places a symmetrical pair of buy-stop and sell-stop orders around the most recent candle. Once a breakout order fills, the opposite pending order is removed and the position is managed with fixed stops, optional take-profit levels, and a trailing routine that first locks in a small profit at break-even and then follows price.

## Strategy logic
### Engulfing pattern detection
The original expert advisor exposes two entry blocks and both are preserved:
* **Type 1 setup** – scans the previous `Max Search` candles (skipping the current bar) and waits for the lowest low within that range to occur two bars ago **or** for the highest high to occur two bars ago. When that happens the candle two bars back must engulf the previous candle (higher high and lower low). The setup arms a straddle around the most recent finished candle.
* **Type 2 setup** – also scans the previous `Max Search` candles, but looks for the extreme to appear one bar ago. In addition, the candle one bar ago must engulf the candle two bars back. A straddle is then placed around the most recent candle. Both setups can coexist; each manages its own pending orders and expiration clock.

### Pending order placement
* **Entry prices** – buy-stop orders are placed at the previous candle high plus `Gap Points`, sell-stop orders at the previous candle low minus `Gap Points`.
* **Stop-loss** – for Type 1 the long stop is anchored at the low of the candle two bars back (minus the gap) and the short stop at the high of that candle (plus the gap). Type 2 uses the previous candle for both sides.
* **Take-profit** – optional. Long targets add `Gap Points + Buy Take Profit Points` to the previous high, and shorts subtract `Gap Points + Sell Take Profit Points` from the previous low. Setting the take-profit inputs to `0` disables the respective targets.
* **Expiration** – each straddle carries a validity timestamp computed as `Order Expiry (bars)` multiplied by the configured candle timeframe. If the pending orders are still working when the timestamp is reached, both sides are cancelled.

### Position management
* Once a buy-stop fills, any remaining sell-stop orders from either setup are cancelled; the symmetric rule applies after a short entry.
* Stops and targets are monitored on completed candles. If a candle’s low reaches the long stop (or the high reaches the short stop) the position is closed with a market order. The same approach is used for the take-profit levels.
* The break-even routine (`Break-even Trigger` and `Break-even Offset`) moves the stop to the entry price plus/minus the configured offset once the position advances by the trigger amount.
* The trailing block keeps the stop `Long/Short Trailing Buffer` points away from the best excursion, but only after price has travelled far enough (and optionally only after the trade is already profitable). `Trailing Step` prevents over-frequent adjustments by requiring a minimum improvement before the stop is tightened again.

## Parameters
* **Volume** – order volume for each pending stop order.
* **Buy Take Profit (points)** – distance in points used to compute the long take-profit (set to `0` to disable).
* **Sell Take Profit (points)** – distance in points used to compute the short take-profit (set to `0` to disable).
* **Gap (points)** – buffer added to highs/lows before placing stop entries and folded into the take-profit distance.
* **Search Depth** – number of finished candles scanned when checking for Type 1 and Type 2 engulfing setups.
* **Order Expiry (bars)** – number of candle lengths a pending straddle remains active before both sides are cancelled.
* **Break-even Trigger (points)** – profit threshold that arms the break-even stop adjustment.
* **Break-even Offset (points)** – additional buffer added to the entry price when the break-even stop is placed.
* **Long Trailing Buffer (points)** – trailing distance for long positions once break-even has been reached.
* **Short Trailing Buffer (points)** – trailing distance for short positions once break-even has been reached.
* **Trailing Step (points)** – minimum improvement in stop location required before updating the trailing stop again.
* **Trail Only After Profit** – if enabled, trailing waits until the position has moved beyond the buffer before activating.
* **Candle Type** – timeframe of the candles used for pattern detection, order expiry, and exit processing.

## Additional notes
* Price offsets expressed in “points” rely on the security’s `PriceStep`. Symbols with five (or three) decimal places automatically convert to fractional pip sizes just like in MetaTrader.
* Stop losses and take profits are executed via market orders inside the strategy to mirror the EA’s behaviour of managing levels on closed candles.
* The strategy does not implement the unused `vhod_3` function from the original source; only the two active entry blocks were ported.
* This package contains only the C# implementation; no Python version is provided.
