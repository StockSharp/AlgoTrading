# Ichimoku Cloud Retrace Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the MetaTrader expert "ichimok2005". It looks for pullbacks into the Ichimoku cloud and trades in the direction of the prevailing kumo slope. Signals are evaluated only on completed candles.

## Overview

- Works on any instrument and timeframe that provides candle data.
- Uses the standard Ichimoku settings (9/26/52) by default but they are fully configurable.
- Trades both long and short. Position size is defined by the strategy `Volume` property.
- Optional stop-loss and take-profit can be configured in absolute price units.

## Indicators and Parameters

- **Ichimoku**: `Tenkan`, `Kijun`, and `Senkou Span B` lengths are exposed via parameters.
- **Candle Type**: choose any aggregated candle type supported by the connection (default: 1 hour time frame).
- **Stop Loss Offset**: optional distance below/above the entry price that forces an exit. Set to `0` to disable.
- **Take Profit Offset**: optional profit target distance from the entry price. Set to `0` to disable.

## Entry Rules

### Long Setup

1. `Senkou Span A` is above `Senkou Span B`, signalling a bullish cloud.
2. The current finished candle is bullish (`Close > Open`).
3. The candle closes inside the cloud (`Close` is between the two spans).
4. When all conditions align and the strategy is flat or short, it sends a market buy order sized to both close any short exposure and open a new long.

### Short Setup

1. `Senkou Span B` is above `Senkou Span A`, signalling a bearish cloud.
2. The current finished candle is bearish (`Open > Close`).
3. The candle closes inside the cloud (`Close` is between the two spans).
4. When the conditions align and the strategy is flat or long, it sends a market sell order sized to both close any long exposure and open a new short.

## Exit Rules

- Opposite signals automatically reverse the position by combining the close and the new entry into a single market order.
- When enabled, `Stop Loss Offset` exits at `EntryPrice - Offset` for longs and `EntryPrice + Offset` for shorts, using the candle close price.
- When enabled, `Take Profit Offset` exits at `EntryPrice + Offset` for longs and `EntryPrice - Offset` for shorts.
- Manual flatting (closing the strategy) also resets the internal entry price tracker.

## Risk Management Notes

- Offsets are expressed in absolute price units. Convert pip or tick distances to price before configuring them.
- Because the strategy uses candle close prices for risk checks, consider tighter offsets for lower time frames.
- No trailing or partial exits are implemented; the strategy always exits the whole position.

## Additional Implementation Details

- The strategy subscribes to candles through the high-level API and binds the Ichimoku indicator with `BindEx`.
- Only finished candles trigger logic; intermediate updates are ignored.
- A chart area is created automatically (when available) to display price, the Ichimoku cloud, and the executed trades.
- `ManageRisk` is executed before looking for new entries so that protective exits have priority.
