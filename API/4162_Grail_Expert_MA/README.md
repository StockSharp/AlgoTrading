# Grail Expert MA

## Overview
Grail Expert MA is a StockSharp port of the MetaTrader 4 expert advisor `_GrailExpertMAV1_0`. The system searches for fresh breakouts beyond the recent high/low channel and waits for a pullback before joining the move. An exponential moving average of the typical price provides the directional bias: trades are only allowed when the EMA has gained or lost a configurable number of pips across the last two completed candles. Risk management mirrors the original expert with pip-based stop-loss and take-profit distances and ignores new entries while a position is active.

## Strategy logic
### EMA slope trend filter
* An EMA calculated on the typical price ((High + Low + Close)/3) is evaluated at the close of every bar.
* The difference between the last two EMA values must exceed the `EMA Slope (pips)` threshold (converted to price using the symbol pip size).
* A positive slope authorizes long pullbacks, a negative slope authorizes short pullbacks, and flat slopes block trading.

### Breakout range tracking
* The strategy maintains the highest high and lowest low across the last `Range Period` completed bars.
* These levels form a channel whose height is used to reject shallow moves that do not create enough distance for the pullback logic.

### Entry preparation
* When the current bar prints a new high above the stored range, a potential long entry price is computed at `High - Breakout Buffer - Take Profit` pips.
* When the current bar prints a new low below the stored range, a potential short entry price is computed at `Low + Breakout Buffer + Take Profit` pips.
* The original EA required the distance between the new extreme and the opposite side of the range to be at least `2 * Breakout Buffer + Take Profit`. The port keeps the same validation and discards the entry if the spread is too small.

### Entry trigger
* Prepared prices remain active for the rest of the bar. A long is executed when the intrabar low reaches or dips below the stored long entry price while the EMA slope is positive.
* A short is executed when the intrabar high reaches or exceeds the stored short entry price while the EMA slope is negative.
* Only one trade can be open at a time; the port clears both pending entry prices as soon as an order is submitted to match the MQL behaviour.

### Exit management
* Long positions use a stop at `Entry - Stop Loss` pips and a profit target at `Entry + Take Profit` pips (zero disables the respective level).
* Short positions mirror the calculations (stop above, target below).
* Exits are triggered when candle extremes touch the protective levels, matching the bar-based approximation of the original tick logic.

### Additional safeguards
* Pending entries are cleared whenever they fall outside the refreshed range when a new candle closes.
* All pip distances automatically adapt to the instrument’s tick size (five-digit FX symbols map one pip to 10 ticks).
* If the EMA is not yet formed or the range buffer lacks enough history, the strategy remains idle until sufficient data is available.

## Parameters
* **Order Volume** – trade volume in lots/contracts for market orders.
* **Take Profit (pips)** – distance to the fixed profit target; set to `0` to disable.
* **Stop Loss (pips)** – distance to the protective stop; set to `0` to disable.
* **Range Period** – number of completed candles used to measure the breakout channel.
* **EMA Period** – length of the exponential moving average applied to the typical price.
* **EMA Slope (pips)** – minimum pip advance/decline between consecutive EMA values required to enable entries.
* **Breakout Buffer (pips)** – additional distance away from the fresh extreme before arming pullback entries.
* **Candle Type** – timeframe requested from the data feed (default: 1-hour candles).

## Implementation notes
* The strategy uses raw candle updates (including partial states) to emulate the original intrabar high/low monitoring.
* EMA values are processed only on finished candles to replicate the MQL `iMA` calls with shifts of one and two bars.
* Historical ranges are tracked with bounded queues instead of indicator lookups to avoid expensive rescans while keeping the logic faithful to the source.
* No Python version is provided; the API package contains only the C# implementation.
