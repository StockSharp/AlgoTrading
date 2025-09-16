# Maximus vX Lite Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Conversion of the "maximus_vX lite" MetaTrader 5 expert advisor to the StockSharp high-level API.
The strategy searches for consolidation zones above and below the current price and waits for price to move a configurable
number of points away from those zones before entering. Position size is determined from an optional risk-percentage budget,
and floating profit can trigger a forced liquidation of all open exposure.

## Strategy Logic

1. **Historical scan** – on every finished candle the strategy keeps up to `HistoryDepth` candles and uses a sliding
   `RangeLookback` window to detect compact highs and lows that build consolidation areas.
2. **Upper channel** – when a valid upper block is detected the channel is anchored around the current close with a
   width of `RangePoints`. If no historical block qualifies, the channel falls back to the same width snapped to the
   current price.
3. **Lower channel** – the lower block is either taken directly from historical highs/lows that satisfy the range
   conditions or, if none exist, from a synthetic level around the current close minus `RangePoints`.
4. **Long entries** – two long setups are allowed:
   - Break above the lower consolidation: price must exceed `_lowerMax` by `DistancePoints` and the upper channel must be
     available. The take profit uses two thirds of the distance between `_lowerMax` and `_upperMin`, with a minimum equal to
     `RangePoints`.
   - Break above the upper channel: price must exceed `_upperMax` by `DistancePoints`. The take profit is set to
     `2 * RangePoints`.
5. **Short entries** – symmetric logic fires when price drops below `_upperMin` or `_lowerMin` by `DistancePoints`.
   The primary short setup also uses the dynamic two-thirds target while the secondary one uses `2 * RangePoints`.
6. **Stops and exits** – `StopLossPoints` define a fixed protective stop when greater than zero. `MinProfitPercent` monitors
   floating equity versus last flat balance and closes all positions once the threshold is exceeded. Manual stop/target checks
   emulate the original expert advisor behaviour inside the strategy.
7. **Position sizing** – when `RiskPercent` is greater than zero and a stop is defined, order volume is calculated from the
   portfolio value and the stop distance. Otherwise the strategy reuses the `Volume` property.

## Parameters

- `DelayOpen` (default `2`) – number of timeframe bars during which adding to the same side is allowed.
- `DistancePoints` (default `850`) – minimum distance from a consolidation border before entering.
- `RangePoints` (default `500`) – width of the consolidation boxes.
- `HistoryDepth` (default `1000`) – number of candles kept in memory for historical scans.
- `RangeLookback` (default `40`) – window length used to compute local maxima and minima.
- `CandleType` (default `TimeSpan.FromMinutes(15).TimeFrame()`) – timeframe used for calculations.
- `RiskPercent` (default `5m`) – percent of portfolio value risked per trade. Set to zero to use fixed volume.
- `StopLossPoints` (default `1000`) – protective stop distance; zero disables the stop.
- `MinProfitPercent` (default `1m`) – floating profit percentage that forces all positions to close.

## Details

- **Long/Short**: Both directions
- **Exit Criteria**: Fixed stop or take profit, equity lock via `MinProfitPercent`
- **Stops**: Optional fixed stop from `StopLossPoints`
- **Indicators**: None (pure price action with sliding-window analysis)
- **Timeframe**: Configurable via `CandleType` (default 15 minutes)
- **Complexity**: Intermediate (combines history scanning, dynamic targets, and risk sizing)
- **Risk Level**: High when risk percentage is used due to breakout nature
