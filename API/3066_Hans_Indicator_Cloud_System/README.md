# Hans Indicator Cloud System Strategy

## Overview
This strategy ports the MQL5 expert advisor `Exp_Hans_Indicator_Cloud_System` to the StockSharp high-level API. It reproduces the
Hans indicator "cloud" ranges that divide each trading day into two reference sessions and trades when the indicator reports a
breakout above or below those dynamic ranges. The implementation consumes a configurable candle series (default: M30), processes
only finished candles, and mirrors the delayed execution logic from the original script by acting on the next bar after a colour
change.

## Hans indicator recreation
The original indicator shifts all timestamps from the broker timezone (`LocalTimeZone`) to a target timezone (`DestinationTimeZone`).
The StockSharp port applies the same offset before splitting every day into two sessions:

1. **Session 1 (04:00–08:00 target time)** – the strategy records the highest high and lowest low of all candles that fall inside
   this window. Once the window ends the zone is considered complete.
2. **Session 2 (08:00–12:00 target time)** – the process repeats for the second window. When this session finishes its high/low
   values supersede the first zone for the rest of the day.

A configurable buffer (`PipsForEntry`) expressed in price steps is added above the high and below the low of the active zone. The
indicator colour map is reproduced as follows:

- `0` – close is above the upper zone and the candle body is bullish.
- `1` – close is above the upper zone and the candle body is bearish.
- `3` – close is below the lower zone and the candle body is bullish.
- `4` – close is below the lower zone and the candle body is bearish.
- `2` – no breakout (neutral state).

These values are stored to emulate the `CopyBuffer` look-ups performed by the MQL5 expert.

## Trading logic
- The strategy keeps a rolling history of colour codes and looks back `SignalBar` bars (default 1) plus one extra bar, matching the
  `CopyBuffer(..., SignalBar, 2, ...)` call from the source.
- **Open long**: the older bar (`SignalBar + 1`) reports colour `0` or `1` and the more recent bar (`SignalBar`) is not coloured
  `0`/`1`. Any existing short exposure is closed before opening a new long of `TradeVolume` units.
- **Open short**: the older bar reports colour `3` or `4` and the more recent bar is not coloured `3`/`4`. Any existing long
  exposure is flattened first and then a new short is opened.
- **Close long**: whenever the older bar is coloured `3` or `4` and long exits are enabled.
- **Close short**: whenever the older bar is coloured `0` or `1` and short exits are enabled.

Exits are processed before entries exactly like the helper functions inside `TradeAlgorithms.mqh`, ensuring that opposite
positions are closed prior to issuing fresh orders.

## Parameters
- **Candle type** (`CandleType`): timeframe of the processed candles.
- **Signal bar** (`SignalBar`): how many finished candles back to inspect for a colour change.
- **Local timezone** (`LocalTimeZone`): broker/server timezone in hours.
- **Destination timezone** (`DestinationTimeZone`): target timezone that defines the session windows.
- **Breakout buffer** (`PipsForEntry`): number of price steps added above/below the detected session range.
- **Enable long entries/exits** (`BuyPosOpen`, `BuyPosClose`): toggles for managing long positions.
- **Enable short entries/exits** (`SellPosOpen`, `SellPosClose`): toggles for managing short positions.
- **Trade volume** (`TradeVolume`): order size used for every new position; also synced with `Strategy.Volume` on start.

## Notes
- Python translation is intentionally omitted as requested.
- The money-management helpers from `TradeAlgorithms.mqh` (margin modes, dynamic position sizing, stop-loss/ take-profit
  placement) are simplified to a fixed trade volume and explicit exit rules.
- When the security does not expose `PriceStep` the breakout buffer is interpreted as absolute price units, matching the best
  approximation available without tick-size information.
