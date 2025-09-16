# Rabbit M3

## Overview
Rabbit M3 is a port of the MetaTrader 4 expert advisor `RabbitM3` (also released under the name "Petes Party Trick"). The system flips between long-only and short-only regimes using a pair of hourly exponential moving averages. Momentum confirmation comes from a Williams %R cross combined with a CCI level filter, while an extremely long Donchian channel watches for price breakouts that invalidate the current trend bias. Position size can optionally grow after large winners, replicating the lot-scaling rule contained in the original code.

## Strategy logic
### Trend regime filter
* When the fast EMA closes below the slow EMA, any existing long exposure is liquidated and new signals are restricted to the short side.
* When the fast EMA closes above the slow EMA, any existing short exposure is closed and only long setups remain eligible.
* If the EMAs are equal the previous regime is kept, mirroring the MetaTrader logic that only toggles on strict inequalities.

### Entry rules
* **Short trades**
  * Regime must be short-only (fast EMA below slow EMA).
  * Williams %R (length = `WilliamsPeriod`) must cross down through the `WilliamsSellLevel` on the most recent candle while the previous value was still below zero.
  * CCI (length = `CciPeriod`) must be greater than or equal to `CciSellLevel`.
  * Net position must be flat; the strategy opens at most `MaxOpenPositions` trades and defaults to a single market order of size `EntryVolume`.
* **Long trades**
  * Regime must be long-only (fast EMA above slow EMA).
  * Williams %R must cross up through the `WilliamsBuyLevel` while the previous value was still below zero.
  * CCI must be less than or equal to `CciBuyLevel`.
  * Net position must be flat before a new long is initiated.

### Exit rules
* **Hard stops** – `StopLossPips` and `TakeProfitPips` are converted into price offsets using the instrument’s price step. A value of `0` disables the corresponding protective level.
* **Donchian breakout** – if price closes above the previous Donchian upper band (length = `DonchianLength`) any short position is closed immediately. A close below the previous lower band closes longs. The channel uses the prior completed value to reproduce the `iHighest`/`iLowest` lag from the EA.
* **Regime flip** – whenever the EMA relationship reverses the strategy liquidates the opposing exposure before allowing new trades in the fresh direction.

### Money management
* Starts with `EntryVolume` units per trade.
* When a realized profit greater than `BigWinThreshold` occurs while the strategy is flat, volume is increased by `VolumeIncrement` and the threshold doubles (4 → 8 → 16, etc.). If either parameter is set to `0` the scaling rule is disabled.

## Parameters
* **Fast EMA Period** – length of the fast trend filter (default: 33).
* **Slow EMA Period** – length of the slow trend filter (default: 70).
* **Williams %R Period** – lookback for the Williams %R oscillator (default: 62).
* **Williams Sell Level** – upper bound that must be crossed downward for short signals (default: −20).
* **Williams Buy Level** – lower bound that must be crossed upward for long signals (default: −80).
* **CCI Period** – lookback for the Commodity Channel Index (default: 26).
* **CCI Sell Level** – minimum CCI value required to permit shorts (default: 101).
* **CCI Buy Level** – maximum CCI value required to permit longs (default: 99).
* **Donchian Length** – number of completed candles sampled for the breakout exit (default: 410).
* **Max Open Positions** – maximum simultaneous trades; the classic setup uses one contract (default: 1).
* **Take Profit (pips)** – profit target measured in price steps (default: 360).
* **Stop Loss (pips)** – protective stop measured in price steps (default: 20).
* **Entry Volume** – starting order size (default: 0.01).
* **Big Win Threshold** – realized profit required before increasing size (default: 4.0).
* **Volume Increment** – additional volume added after beating the threshold (default: 0.01).
* **Candle Type** – timeframe used for all indicator calculations (default: hourly candles).

## Additional notes
* Pip conversion relies on the security’s `PriceStep`. Instruments without a price step fall back to a unitary pip value.
* Donchian levels are intentionally lagged by one candle so the exit mirrors the `shift=1` logic of the original MetaTrader calls.
* Volume scaling only evaluates realized PnL while the position is flat, preventing floating gains from triggering false positives.
* The UI label objects present in the source EA are omitted because StockSharp visualizes state through charts and logs.
* Only the C# implementation is provided in this package; there is no Python version.
