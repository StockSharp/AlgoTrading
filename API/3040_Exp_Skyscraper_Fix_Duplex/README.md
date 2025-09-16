# Exp Skyscraper Fix Duplex Strategy

## Overview
Exp Skyscraper Fix Duplex is a port of the MQL5 expert advisor *Exp_Skyscraper_Fix_Duplex*. The strategy runs the Skyscraper Fix channel on the long and short sides independently, allowing every side to use its own timeframe, ATR window and sensitivity. Long and short trades can therefore react to different market regimes while sharing the same execution logic inside StockSharp.

## Indicator logic
The custom **Skyscraper Fix** indicator reproduces the original script:

- An ATR with a fixed internal period of 15 is calculated for every finished candle.
- The highest and lowest ATR values across the configurable `Length` window determine the adaptive price step.
- Depending on the selected `Mode`, either the bar`s high/low or the close price is used to project upper and lower channel levels at twice the step distance.
- The most recent breakout above the upper level or below the lower level flips the internal trend and clamps the trailing level so that it never moves against the current bias.
- Crossing of the opposing trail produces discrete buy or sell triggers (mirroring the indicator`s arrow buffers in MQL).

The indicator exposes the trailing upper level, trailing lower level, entry triggers and a midline that can be plotted if desired.

## Trading rules
Long and short operations are evaluated separately for each finished candle of the respective subscription:

- **Long entry** – triggered when the long indicator reports a fresh buy level. Existing short exposure is covered first, then a new long market order is submitted with the configured volume.
- **Long exit** – triggered when the long indicator reports the opposing trailing line. Any existing long position is closed with a market sell.
- **Short entry** – triggered when the short indicator reports a fresh sell level. Existing long exposure is closed first, then a new short market order is sent.
- **Short exit** – triggered when the short indicator reports the opposing trailing line. Any active short position is covered with a market buy.

Signals can be delayed with the `SignalBar` parameters so that the strategy acts on the most recently closed candle (`0`) or on candles further back in history (`1` mimics the default MQL setup).

## Parameters
- `TradeVolume` – order size for market entries.
- `EnableLongEntries` / `EnableLongExits` – toggles for long-side trading.
- `LongCandleType` – candle series used for the long indicator.
- `LongLength`, `LongKv`, `LongPercentage`, `LongMode`, `LongSignalBar` – Skyscraper Fix settings for the long side.
- `EnableShortEntries` / `EnableShortExits` – toggles for short-side trading.
- `ShortCandleType` – candle series used for the short indicator.
- `ShortLength`, `ShortKv`, `ShortPercentage`, `ShortMode`, `ShortSignalBar` – Skyscraper Fix settings for the short side.

## Usage notes
- The strategy sets the global `Volume` property from `TradeVolume`, so standard `BuyMarket()` and `SellMarket()` calls use that size automatically.
- Both indicator instances read the instrument`s `PriceStep`. If it is zero the indicator waits silently until a valid price step becomes available.
- `StartProtection()` is invoked on start so platform-level protections are active before the first order is submitted.
- There is no separate Python implementation; the `PY` directory is intentionally omitted as requested.
