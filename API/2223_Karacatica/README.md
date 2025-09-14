# Karacatica Strategy

## Overview
The Karacatica strategy is a trend-following approach that combines price action with the Average Directional Index (ADX). It looks for situations where the current close price is higher or lower than the close price a specified number of candles ago and confirms the move with the dominance of the +DI or -DI line.

## Indicators
- **Average Directional Index (ADX)** – measures trend strength and provides +DI and -DI components.
- **Price Comparison** – checks whether the latest close is above or below the close from *Period* candles back.

## Parameters
- `Period` – number of candles used for both the ADX calculation and the lookback for the price comparison. Default is 70.
- `TakeProfitPercent` – take-profit expressed as a percentage of the entry price. Default is 2%.
- `StopLossPercent` – stop-loss expressed as a percentage of the entry price. Default is 1%.
- `CandleType` – timeframe of candles to subscribe to. Default is 1 hour.

## Trading Logic
- **Long Entry**: `Close > Close[Period]` and `+DI > -DI` with no existing long signal. Closes short positions and opens a long one.
- **Short Entry**: `Close < Close[Period]` and `-DI > +DI` with no existing short signal. Closes long positions and opens a short one.
- **Position Protection**: `StartProtection` applies both take-profit and stop-loss percentages.

## Usage Notes
- Designed for StockSharp high-level API; it subscribes to candles and binds the ADX indicator.
- The strategy automatically closes opposite positions when a new signal appears.
- No Python implementation is provided for now.

## Disclaimer
This example is for educational purposes only and does not guarantee profits. Trading involves significant risk. Always test strategies thoroughly before deploying them on live markets.
