# Candle Patterns Test Strategy

## Overview

The **Candle Patterns Test Strategy** is a StockSharp high-level conversion of the original MetaTrader 5 expert advisor *CandlePatternsTest EA*. The strategy scans completed candles for a curated list of classical Japanese candlestick formations and reacts by entering long or short positions when bullish or bearish structures appear. The conversion focuses on the discretionary pattern logic of the source robot while leveraging StockSharp risk controls and data subscription API.

## Trading Logic

1. **Candle subscription** – the strategy subscribes to the configured candle type and waits for finished bars before running pattern recognition.
2. **Average body filter** – a simple moving average of candle bodies acts as dynamic normalization. Only patterns whose constituent candles exceed this average are considered valid, mirroring the MQL implementation's `AvgBody` function.
3. **Pattern recognition** – the detector checks for:
   - Three White Soldiers / Three Black Crows
   - Piercing Line / Dark Cloud Cover
   - Morning Doji Star / Evening Doji Star
   - Bullish and Bearish Engulfing
   - Bullish and Bearish Harami
   - Meeting Lines
4. **Entry management** – once a bullish pattern is confirmed the strategy opens a market buy order; bearish patterns trigger a market sell order. Opposite signals automatically reverse the current position.
5. **Exit management** – protective stop-loss and take-profit levels are derived from the average candle body and tracked on each finished candle. If price touches either threshold the position is closed.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Data type of candles to subscribe to (default: 1-hour time frame). |
| `AverageBodyPeriod` | Number of candles used for the average body length. Controls pattern normalization. |
| `EnableBullishPatterns` | Enables or disables long entries. |
| `EnableBearishPatterns` | Enables or disables short entries. |
| `StopLossFactor` | Multiplier applied to the average body for stop-loss distance. |
| `TakeProfitFactor` | Multiplier applied to the average body for take-profit distance. |

All parameters are exposed through `StrategyParam<T>` to support GUI configuration and optimizer runs.

## Charting

When a chart area is available the strategy plots:

- The subscribed candles
- The close-price moving average used for trend context
- Executed trades for visual verification

## Differences from the Original EA

- News filters, time windows, hedging toggles, and trailing grid management present in the original MQ5 file are intentionally omitted to focus on the candlestick pattern core.
- Risk management is simplified to a symmetric stop/target model derived from candle volatility.
- The StockSharp version uses the framework's position management and `BuyMarket`/`SellMarket` helpers instead of manual order tickets.

## Usage Notes

- Set the `CandleType` parameter to align with the market session you want to analyze; higher time frames produce fewer but stronger signals.
- Adjust `AverageBodyPeriod` so that the average body approximates recent volatility. A smaller value reacts faster but may increase noise.
- `StopLossFactor` and `TakeProfitFactor` can be optimized to match the instrument's risk profile.

## Requirements

- StockSharp environment with market data feed capable of generating the configured candle type.
- The strategy expects sequential, non-overlapping candle series. Ensure the selected board supports regular bar updates.
