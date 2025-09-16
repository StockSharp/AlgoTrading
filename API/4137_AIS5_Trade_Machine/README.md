# AIS5 Trade Machine

## Overview
AIS5 Trade Machine ports the MetaTrader 4 expert advisor `AIS5TM.mq4` into the StockSharp high-level strategy API. The original
program focused on building market profile histograms on two timeframes and offering a semi-automatic execution console. The
StockSharp version keeps the idea of highlighting strong and weak price zones from tick volume aggregation and turns it into an
automated breakout system with adaptive risk control based on the Average True Range (ATR).

The strategy subscribes to two candle streams:
* A **profile timeframe** (default 15 minutes) that accumulates volume to detect strong and weak zones.
* A **trading timeframe** (default 1 minute) that looks for volume-confirmed breakouts away from those zones.

Positions are protected by ATR-proportional stops and scalable targets. Volume contractions trigger early exits to mimic the
monitoring discipline found in the MT4 code.

## Strategy logic
### Volume zone detection (profile timeframe)
* Each finished higher-timeframe candle updates two simple moving averages (SMA) of tick volume.
* A candle is labeled a **strong zone** when its volume exceeds the average by the configurable multiplier (`Strong Volume Mult`).
  The closing price of the candle becomes the most recent strong level.
* A candle is labeled a **weak zone** when its volume falls below the average divided by the configured divider
  (`Weak Volume Divider`). The closing price of that candle becomes the latest weak level.
* Only finished candles participate. The strategy ignores zones until the profile SMA is fully formed to avoid premature
  signals during the warm-up period.

### Breakout entries (trading timeframe)
* The lower timeframe waits for both its volume SMA and the ATR indicator to finish forming.
* A long setup requires the close to exceed the most recent strong level by the sum of the **Zone Base Points** and
  **Zone Step Points** buffers (converted via the instrument's price step). The candle must also deliver a volume spike relative
  to the intraday average.
* A short setup mirrors the logic around the latest weak level, requiring a breakdown beyond the combined buffer and confirming
  volume expansion.
* The original MT4 expert allowed manual commands and multi-order grids. The StockSharp port keeps a single-position model, so a
  breakout is only acted upon when the current net position is flat.

### Exit management
* On entry the strategy stores the fill price, computes an ATR-based protective stop (ATR multiplied by `ATR Multiplier` and
  clamped by the base zone buffer), and sets the target as the stop distance multiplied by the weak volume divider. This keeps
  risk and reward aligned with the volume structure.
* While a position is open the strategy monitors each finished trading candle:
  * If price reaches the profit target or the protective stop, the position is flattened immediately.
  * If tick volume contracts below the weak-volume threshold before either level is hit, the trade is closed early to avoid
    lingering in inactive zones.
* When the net position returns to zero the internal state resets, allowing the next breakout to be evaluated from scratch.

## Parameters
* **Profile Candle** – candle type that feeds the volume profile (default: 15-minute candles).
* **Trading Candle** – lower timeframe used for breakouts and exits (default: 1-minute candles).
* **Volume Lookback** – number of candles for both volume SMAs and the ATR period.
* **Strong Volume Mult** – multiplier above the average volume that marks a strong zone (maps to `Parameter.1` in MQL).
* **Weak Volume Divider** – divider below average volume that marks weak zones and sizes the profit target (maps to
  `Parameter.2`).
* **ATR Multiplier** – scale factor applied to ATR when computing the adaptive stop distance (maps to `Parameter.3`).
* **Zone Base Points** – minimum buffer in points added to the zone level before checking breakouts (maps to `ZoneBasePoints`).
* **Zone Step Points** – additional breakout buffer in points that widens the distance away from the zone before entries are
  triggered (maps to `ZoneStepPoints`).
* **Volume** – inherited from the base `Strategy` class; defines the order size for market entries.

## Additional notes
* The strategy automatically falls back to a default price step of `0.0001` if the security does not specify one. This keeps the
  point-based parameters compatible with most FX symbols.
* All indicator calculations rely on finished candles to match the MT4 implementation that worked on fully closed bars.
* Unlike the original EA, there is no manual control panel or file-based profile loader. Zones are rebuilt purely from live
  candle data to keep the port self-contained.
* The StockSharp version does not include a Python translation.
