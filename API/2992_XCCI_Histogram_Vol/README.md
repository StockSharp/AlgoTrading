# XCCI Histogram Vol Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor `Exp_XCCI_Histogram_Vol`. It reproduces the color-coded logic of the custom "XCCI Histogram Vol" indicator: a Commodity Channel Index (CCI) multiplied by volume, smoothed by a selectable moving average, and compared to dynamic thresholds. The implementation follows the high level API guidelines, processes closed candles only, and keeps the original dual-position structure by exposing separate volumes for the primary and secondary entries.

## Indicator workflow
1. Calculate the CCI value with the configurable period.
2. Multiply the CCI value by the candle volume.
3. Smooth both the CCI×Volume series and the raw volume with the chosen moving average (`Simple`, `Exponential`, `Smoothed`, `Weighted`, `Hull`, or `VolumeWeighted`).
4. Scale four user-defined threshold multipliers (HighLevel2/1 and LowLevel1/2) by the smoothed volume.
5. Classify the smoothed CCI×Volume value into one of five zones: `0` extreme bullish, `1` bullish, `2` neutral, `3` bearish, `4` extreme bearish.

The strategy stores the zone for every finished candle. The `SignalBarOffset` parameter controls how many fully closed candles to wait before using the zone in trading decisions (mirroring the original `SignalBar` input).

## Trading rules
- **Long exits**: if the evaluated zone is `3` or `4`, every open long position is closed.
- **Short exits**: if the evaluated zone is `1` or `0`, every open short position is closed.
- **Primary long entry**: trigger when the current zone becomes `1` and the previous zone (older candle) was above `1`. This mirrors the transition from neutral/bearish territory into the bullish band. The order volume is `PrimaryEntryVolume` and closes any existing short exposure before flipping.
- **Secondary long entry**: trigger when the current zone becomes `0` and the previous zone was above `0`. This represents a surge into the extreme bullish region and uses `SecondaryEntryVolume`.
- **Primary short entry**: trigger when the current zone becomes `3` and the previous zone was below `3`, indicating a fresh move into bearish territory. Uses `PrimaryEntryVolume` and closes longs first if needed.
- **Secondary short entry**: trigger when the current zone becomes `4` and the previous zone was below `4`, signaling an extreme bearish acceleration. Uses `SecondaryEntryVolume`.

Entry flags are reset whenever the net position crosses zero so that the behaviour matches the "two magic numbers" design from MetaTrader—only one order per tier is allowed until the opposite signal or risk module closes the trade.

## Risk management
- `UseStopLoss` / `UseTakeProfit` enable absolute protective distances (expressed in price points) via the built-in `StartProtection` helper. Stops are optional just as in the original code.
- The strategy uses market orders for every action and therefore respects the platform-wide slippage handling configured in StockSharp.
- Logging calls describe every entry and exit, making it easier to audit why a trade was executed.

## Parameters
- **CciPeriod** – length of the Commodity Channel Index.
- **MaLength** – length applied to both smoothing moving averages.
- **HighLevel2 / HighLevel1 / LowLevel1 / LowLevel2** – multipliers applied to the smoothed volume to create adaptive thresholds.
- **SignalBarOffset** – number of closed candles to wait before acting on a zone (0 = last closed candle, 1 = previous candle, etc.).
- **Smoothing** – moving average type used for smoothing (subset of the original options: SMA, EMA, SMMA, WMA, Hull MA, VWMA).
- **AllowLongEntries / AllowShortEntries / AllowLongExits / AllowShortExits** – enable or disable each side independently.
- **PrimaryEntryVolume / SecondaryEntryVolume** – volumes for the two entry tiers (used for both long and short trades).
- **UseStopLoss / StopLossPoints** – optional absolute stop-loss.
- **UseTakeProfit / TakeProfitPoints** – optional absolute take-profit.
- **CandleType** – timeframe (or any other candle data type) requested from the connector.

## Differences from the MetaTrader version
- Only smoothing methods that exist in StockSharp are exposed; exotic filters such as JJMA, JurX, Parabolic MA, VIDYA, and AMA are not included. Pick the closest available alternative if you need similar behaviour.
- Candle volume is taken from `ICandleMessage.TotalVolume`. Tick volume is not emulated. If the underlying connector supplies only trade counts, the result will differ from the original terminal.
- Order management is netted (single position) instead of two independent magic numbers. Separate primary/secondary entry flags emulate the same intent while staying compatible with the StockSharp execution model.
