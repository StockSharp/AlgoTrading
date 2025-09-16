# EF Distance Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp adaptation of the MetaTrader "Exp_EF_distance" expert advisor. It replaces the original EF Distance and Flat-Trend indicators with a simple moving average (SMA) and an Average True Range (ATR) filter to detect market turning points. The algorithm watches three consecutive SMA values and identifies local minima or maxima. A long position is opened when the SMA forms a local bottom and volatility exceeds the threshold. A short position is opened on the opposite pattern. Positions are closed on opposite signals or when stop-loss or take-profit levels are hit.

## Details

- **Entry Criteria**:
  - **Long**: `SMA(t-1) < SMA(t-2)` and `SMA(t) > SMA(t-1)` and `ATR(t) ≥ AtrThreshold`.
  - **Short**: `SMA(t-1) > SMA(t-2)` and `SMA(t) < SMA(t-1)` and `ATR(t) ≥ AtrThreshold`.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Reverse signal in the opposite direction.
  - Stop-loss or take-profit reached.
- **Indicators**:
  - Simple Moving Average (SMA) – approximation of EF Distance.
  - Average True Range (ATR) – volatility filter.
- **Default Values**:
  - `SMA period` = 10.
  - `ATR period` = 20.
  - `ATR threshold` = 1.
  - `StopLoss` = 100.
  - `TakeProfit` = 200.
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Two
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Configurable
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes (uses turning points)
  - Risk level: Medium
