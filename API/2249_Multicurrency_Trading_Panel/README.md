# Multicurrency Trading Panel Strategy

This strategy emulates the behavior of the original MQL "Multicurrency trading panel" expert advisor. It monitors three currency pairs (EURUSD, USDJPY, GBPUSD) and compares the latest candle with the previous one using seven simple metrics (open, high, low, (high+low)/2, close, (high+low+close)/3, (high+low+close+close)/4).
For each comparison, a BUY or SELL score is increased. When automatic trading is enabled, the strategy opens or reverses positions on a pair if BUY score exceeds SELL score or vice versa.

## Parameters
- **EURUSD** – first security.
- **USDJPY** – second security.
- **GBPUSD** – third security.
- **Candle Type** – timeframe of candles.
- **Auto Trade** – toggle to allow automatic order placement.

The strategy is a simplified demo and is not intended for real trading.
