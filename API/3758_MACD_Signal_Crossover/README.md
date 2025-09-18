# MACD Signal Crossover Strategy

## Overview
This sample converts the original MetaTrader 4 expert advisor `MACD_v1.mq4` into a StockSharp high-level strategy. The algorithm tracks moving average convergence divergence (MACD) crossovers and trades in the direction of the new trend. Optional protective exits replicate the original advisor's behaviour: a stop-loss, a distant take-profit and a partial profit target that liquidates half of the current position.

## Trading Logic
1. **Data source** – the strategy subscribes to the configured candle series (5-minute candles by default) and binds a `MovingAverageConvergenceDivergenceSignal` indicator.
2. **Entry conditions**:
   - Enter **long** when the MACD line crosses above the signal line. If a short position is active, it is closed before opening the long.
   - Enter **short** when the MACD line crosses below the signal line. If a long position exists, it is closed first.
3. **Exit conditions**:
   - Opposite MACD crossover closes the current position and opens a new position in the opposite direction.
   - A configurable take-profit and stop-loss managed by `StartProtection` mirror the original TP/SL parameters (measured in instrument points).
   - A partial profit target closes half of the open position once price advances by a specified amount from the entry level. The partial exit is triggered only once per position.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| **Fast Period** | 23 | Fast EMA length for the MACD calculation (mirrors the MQL parameter `a = 2300`). |
| **Slow Period** | 40 | Slow EMA length for the MACD calculation (`b = 4000` in the source). |
| **Signal Period** | 8 | Signal line length (`c = 800` in the source). |
| **Take Profit** | 500 | Distance in price points for the protective take-profit order. Set to `0` to disable. |
| **Stop Loss** | 80 | Distance in price points for the protective stop-loss order. Set to `0` to disable. |
| **Partial Profit** | 70 | Distance in price points to close half of the open position. Set to `0` to disable partial exits. |
| **Candle Type** | 5-minute time frame | Candle series used for indicator calculations.

## Notes
- Indicator parameters were scaled to typical MACD lengths (23/40/8) because the MQL script expressed them as hundredths (2300/4000/800).
- The strategy automatically restores the partial exit flag whenever a new position is accumulated.
- Chart drawing helpers highlight candles, MACD values and the strategy's trades when a chart panel is available.
- Volume handling relies on the base strategy `Volume` property. Adjust it before starting the strategy to match your instrument size.
