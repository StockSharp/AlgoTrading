# Bykov Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the classic MetaTrader system "Bykov Trend" using the StockSharp high-level API. The original indicator combines the Williams %R oscillator with a simple trend detection mechanism. When the trend flips from bearish to bullish, a long position is opened. When the trend turns from bullish to bearish, a short position is opened.

The system trades a single instrument on a selected timeframe. Only one position is held at a time; opposite signals reverse the position.

## Details

- **Entry Criteria**  
  - **Long**: Williams %R rises above `-K` after being below `-100 + K` (`K = 33 - Risk`).  
  - **Short**: Williams %R falls below `-100 + K` after being above `-K`.
- **Long/Short**: Both directions.  
- **Exit Criteria**  
  - Opposite signal closes the current position and opens a new one in the other direction.  
- **Stops**: None.  
- **Default Values**  
  - `Risk` = 3 (`K = 30`).  
  - `SSP` = 9 (Williams %R look-back).  
  - `CandleType` = 1 hour candles.  
- **Filters**  
  - Category: Trend following  
  - Direction: Both  
  - Indicators: Single (Williams %R)  
  - Stops: No  
  - Complexity: Simple  
  - Timeframe: Flexible  
  - Seasonality: No  
  - Neural networks: No  
  - Divergence: No  
  - Risk level: Medium
