# HMA Seasonal Divergence Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy combines the Hull Moving Average (HMA) with seasonal open interest clustering to find divergences between price and market positioning. It assumes that when price temporarily moves against the direction of rising open interest, a trend continuation is likely. The system is designed to trade both long and short, using the HMA slope to gauge momentum and the seasonal open interest data to measure participation levels.

A trade setup occurs when the HMA changes relative to the previous bar while seasonal open interest confirms the move, but price prints in the opposite direction. This bullish or bearish divergence between price and positioning often signals the end of a short-term pullback within a larger trend. The strategy waits for these conditions before entering and places a volatility-based stop to manage risk.

Positions are closed when the HMA slope reverses, signifying that momentum has shifted. Because the stop level uses a multiple of the Average True Range (ATR), the risk adapts to market volatility. This helps prevent premature exits during periods of expansion and keeps losses contained when volatility contracts.

## Details

- **Entry Criteria**:
  - **Long**: `HMA(t) > HMA(t-1)` && `OI_Cluster_Seasonal(t) > OI_Cluster_Seasonal(t-1)` && `Price(t) < Price(t-1)` (bullish divergence).
  - **Short**: `HMA(t) < HMA(t-1)` && `OI_Cluster_Seasonal(t) < OI_Cluster_Seasonal(t-1)` && `Price(t) > Price(t-1)` (bearish divergence).
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: `HMA(t) < HMA(t-1)` (HMA begins falling).
  - **Short**: `HMA(t) > HMA(t-1)` (HMA begins rising).
- **Stops**: Yes, stop-loss placed at `N * ATR` from entry.
- **Default Values**:
  - `HMA period` = 9.
  - `OI_Cluster_Seasonal` = seasonal OI at cluster levels over five years.
  - `N` = 2 (stop-loss = `2 * ATR`).
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Complex
  - Timeframe: Medium-term
  - Seasonality: Yes
  - Neural networks: Yes
  - Divergence: Yes
  - Risk level: High
