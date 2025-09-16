# Angry Bird Scalping Strategy

This strategy replicates the MetaTrader "Angry Bird (Scalping)" expert advisor using StockSharp's high level API.

## Logic
- Observes 15-minute candles and computes the highest high and lowest low over the last `Depth` bars to derive a dynamic grid step.
- When no position is open and the previous candle closes above the current one, RSI on the hourly timeframe triggers entries: values above `RsiMin` open short positions, values below `RsiMax` open long positions.
- If a position exists and price moves against it by at least the grid step, a new position is opened in the same direction with its volume multiplied by `LotExponent` until `MaxTrades` is reached.
- A strong CCI reading above `CciDrop` for shorts or below `-CciDrop` for longs forces all positions to close.
- Positions are also closed when profit reaches `TakeProfit` or loss reaches `StopLoss` relative to the average entry price.

## Parameters
- `StopLoss` – stop-loss in points.
- `TakeProfit` – take-profit in points.
- `DefaultPips` – minimal distance between grid orders in pips.
- `Depth` – number of candles used for high/low calculation.
- `LotExponent` – multiplier for subsequent order volume.
- `MaxTrades` – maximum number of averaging positions.
- `RsiMin` / `RsiMax` – RSI thresholds for entry.
- `CciDrop` – absolute CCI value forcing position closure.
- `Volume` – initial order volume.
- `CandleType` – timeframe of working candles (default 15 minutes).

## Usage
Attach the strategy to a security and start. The strategy uses market orders and manages a single net position, averaging as price moves against it.
