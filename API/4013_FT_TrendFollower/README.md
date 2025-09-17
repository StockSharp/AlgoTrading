# FT Trend Follower

## Overview
FT Trend Follower is a StockSharp port of the MetaTrader 4 expert advisor `FT_TrendFollower.mq4`. The strategy rides medium-term trends by stacking a Guppy Multiple Moving Average (GMMA) fan with a Laguerre oscillator trigger, a fast/slow EMA crossover and a MACD filter. Entries only fire after the market dips into the GMMA bundle, rebounds from a Laguerre extreme, and the majority of GMMA lines resume sloping in the direction of the trade. Profit management mirrors the original EA: an optional swing-based stop, a fixed-distance stop, and three mutually exclusive staged exit modules driven by daily pivot levels or channel averages.

## Strategy logic
### GMMA structure and trend detection
* The GMMA fan spans from `StartGmmaPeriod` to `EndGmmaPeriod`. Periods are distributed across five groups of `BandsPerGroup` lines each, replicating the original `CountLine` logic.
* Trend direction compares the slower GMMA group (index `CountLine + CountLine` from the end) against the faster long-term group (index `CountLine` from the end). Rising long-term averages define an uptrend; falling ones define a downtrend.
* Slope confirmation counts how many short-, medium-, and long-term GMMA lines increased or decreased versus the previous bar. A trade requires the up- (or down-) slope count to exceed half of the total GMMA lines, mimicking the `controlvverh`/`controlvverhS` threshold in MetaTrader.

### Signal priming
* **Close reset** – When the prior candle closes beneath the slowest GMMA line the long module arms; when it closes above the slowest line the short module arms. Crossing back above (or below) the fastest GMMA clears the arming flags, just like the original `CloseOk` logic.
* **Laguerre trigger** – A Laguerre filter (`LaguerreGamma`) must first fall below `LaguerreOversold` (long setup) or rise above `LaguerreOverbought` (short setup) while the candle still respects the long-term GMMA. Only after the oscillator retreats back through the threshold can an entry fire.
* **EMA crossover** – The fast EMA (`FastSignalLength`) must dip below the slow EMA (`SlowSignalLength`) to arm the long module, and then cross back above it to release the entry. Shorts reverse the inequality.
* **MACD filter** – The MACD main line (5/35/5 as in the EA) must be positive for longs and negative for shorts.

### Entry rules
A long trade is executed when:
1. Trend detection reports an uptrend and the GMMA slope vote exceeds half of the available lines.
2. The Laguerre trigger was previously armed and the current value closes back above `LaguerreOversold`.
3. The fast EMA is above the slow EMA after previously being below.
4. MACD is greater than zero.

Short entries require the symmetrical conditions with the oscillator crossing below `LaguerreOverbought` and MACD negative. When reversing an existing position the order size automatically offsets the prior exposure so the final net position equals `Volume`.

### Risk management and exits
* **Stops** – Choose either the swing stop (`UseSwingStop`) positioned under (over) the previous candle by `SwingStopPips` points, or the fixed-distance stop (`UseFixedStop`) offset by `FixedStopPips` points. If both are enabled at once the strategy aborts at start, reproducing the EA validation rules.
* **Pivot exit module (Quit)** – When enabled, the first partial close (50% of `Volume`) triggers once price crosses the previous day’s R1/S1 pivot with unrealized profit. The remainder closes as soon as the Hull MA produces a valid value, matching the `hma1` buffer check from MetaTrader.
* **Pivot range exit module (Quit1)** – The initial partial close still occurs at R1/S1. The remainder exits at R2/S2 once the trade remains profitable.
* **Channel exit module (Quit2)** – First partial close occurs at R1/S1. The strategy closes the remainder when the candle re-opens below the low SMA channel (`ChannelPeriod`) for longs or above the high SMA channel for shorts, mirroring the original volatility filter.

Only one exit module can be active at a time, just like the EA’s parameter validation.

## Parameters
* **Volume** – Order size for new trades.
* **StartGmmaPeriod / EndGmmaPeriod** – Bounds for the GMMA fan.
* **BandsPerGroup** – Number of GMMA lines sampled per group (CountLine in MT4).
* **FastSignalLength / SlowSignalLength** – EMA lengths used for the crossover confirmation.
* **TradeShift** – Kept for compatibility; the implementation operates on finished candles, so values other than 0 or 1 are rejected.
* **UseSwingStop / SwingStopPips** – Enables and configures the swing-based protective stop.
* **UseFixedStop / FixedStopPips** – Enables the fixed-distance stop measured in price points.
* **EnablePivotExit / EnablePivotRangeExit / EnableChannelExit** – Mutually exclusive staged exit modules.
* **LaguerreOversold / LaguerreOverbought / LaguerreGamma** – Laguerre trigger thresholds and smoothing factor.
* **HmaPeriod** – Hull MA length used by the pivot exit module.
* **ChannelPeriod** – Length of the high/low SMA channel for Quit2.
* **CandleType** – Timeframe driving the strategy calculations (default: 1-hour candles).

## Additional notes
* Daily pivot levels are computed from the last finished daily candle supplied by a secondary subscription.
* Price points and pip conversions rely on the security’s `PriceStep`. Symbols with different tick sizes automatically adapt.
* The strategy subscribes to high-level indicators only and avoids direct buffer reads, adhering to the project’s high-level API guidelines.
* No Python implementation is provided in this package.
